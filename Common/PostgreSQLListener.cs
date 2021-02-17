using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Reflection;

namespace Common
{
    public class PostgreSQLListener<T> : IDisposable where T : new() 
    {
        private readonly string _channel = "db_notifications";
        private string _connstring;
        private NpgsqlConnection _conn;
        private string _table;

        public delegate void ConnErrorHandler(object sender, Exception e);
        public event ConnErrorHandler Error;

        public delegate void NotificationHandler(object sender, PostgreSQLNotification notifyEvent);
        public event NotificationHandler Notification;

        /// <summary>
        /// Connects to the specified PostgreSQL database and listens for the specified notification (where the table name = the name of the generic type T)
        /// </summary>
        /// <param name="connString">The connection string for the database</param>
        /// <param name="tablename">The name of the table to listen for changes to</param>
        public PostgreSQLListener(string connString,string tablename)
        {
            _table = tablename.ToLower();

            // Connect to the database
            _connstring = connString;// + ";Keepalive=1";


            _conn = new NpgsqlConnection(connString);
            try
            {
                _conn.Open();
            }
            catch (Exception ex)
            {
                Error?.Invoke(this,ex);
                return;
            }

            // add handlers for notifications and state changes
            _conn.Notification += _conn_Notification;
            _conn.StateChange += _conn_StateChange;
        }

        private void _conn_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if (Globals.FDAStatus != Globals.AppState.Normal)
                return;

            if ((e.CurrentState == System.Data.ConnectionState.Broken || e.CurrentState == System.Data.ConnectionState.Closed) && e.OriginalState == System.Data.ConnectionState.Open)
                Error?.Invoke(this, new Exception("Database connection lost"));
        }

        public void StartListening()
        {
            if (_conn == null) return;

            if (_conn.State != System.Data.ConnectionState.Open) return;

            // start listening for notifications
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand("listen " + _channel + ";", _conn))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this,ex);
            }
        }

        public void StopListening()
        {
            if (_conn == null) return;
            if (_conn.State != System.Data.ConnectionState.Open) return;

            // start listening for notifications
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand("unlisten " + _channel + ";", _conn))
                {
                    command.ExecuteNonQuery();
                }
            }catch (Exception ex)
            {
                Error?.Invoke(this,ex);
            }

        }


        private void _conn_Notification(object sender, NpgsqlNotificationEventArgs e)
        {

            JObject json = JObject.Parse(e.Payload);

            // get the name of the table that changed, check if it's the table we're looking for
            string table = (string)json["table"];

  
            if (table.ToLower() != _table)
                return;

            string operation = (string)json["operation"];

            DBNotification<T> notificationData = new DBNotification<T>();
            notificationData.operation = operation;
            notificationData.table = (string)json["table"];
            notificationData.schema = (string)json["schema"];
            notificationData.Timestamp = (DateTime)json["timestamp"];


            // get the key column name(s), and the key value(s) of the row that set off the trigger
            string key_columns =((string)json["keycolumns"]).ToUpper();
            string key_values = (string)json["keyvalues"];

            string[] keycolsarray = key_columns.Split(",");
            string[] keyvalsarray = key_values.Split(",");

            PropertyInfo pi;
            if (operation == "DELETE")
            {
                notificationData.row = new T();
                for (int i = 0; i < keycolsarray.Length; i++)
                {
                    pi = notificationData.row.GetType().GetProperty(keycolsarray[i]);
                    if (pi.PropertyType == typeof(Guid))
                        pi.SetValue(notificationData.row, Guid.Parse(keyvalsarray[i]));
                    if (pi.PropertyType == typeof(Int32))
                        pi.SetValue(notificationData.row, Int32.Parse(keyvalsarray[i]));
                }
            }
            else
            {
                string where = " where ";
                for (int i=0;i<keycolsarray.Length;i++)
                {
                    if (i > 0)
                        where += " and ";

                    where += keycolsarray[i] + " = '" + keyvalsarray[i] + "'";
                }

                // query for the row
                string query = "select row_to_json(t) from( select * from " + table + where + ") t;";
                string jsonResult;

                using (NpgsqlConnection conn = new NpgsqlConnection(_connstring))
                {
                    conn.Open();
                    using (NpgsqlCommand command = conn.CreateCommand())
                    {
                        command.CommandText = query;
                        jsonResult = (string)command.ExecuteScalar();
                    }
                }

                // convert the row into an object of type T (very cool! I could use this elsewhere, like in LoadConfig() )
                notificationData.row = JsonConvert.DeserializeObject<T>(jsonResult, new CustomJsonConvert());
            }
            Notification?.Invoke(this, new PostgreSQLNotification(notificationData));
        }

        public void Dispose()
        {
            StopListening();
            _conn?.Close();
            _conn.Dispose();
        }

        private class CustomJsonConvert : JsonConverter
        {
            /// <summary>
            /// Determines whether this instance can convert the specified object type.
            /// </summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns>
            /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
            /// </returns>
            public override bool CanConvert(Type objectType)
            {
                // Handle only boolean types.
                return objectType == typeof(bool);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>
            /// The object value.
            /// </returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.Value.ToString().ToLower().Trim())
                {
                    case "true":
                    case "yes":
                    case "y":
                    case "1":
                        return true;
                    case "false":
                    case "no":
                    case "n":
                    case "0":
                        return false;
                }

                // If we reach here, we're pretty much going to throw an error so let's let Json.NET throw it's pretty-fied error message.
                return new JsonSerializer().Deserialize(reader, objectType);
            }

            /// <summary>
            /// Specifies that this converter will not participate in writing results.
            /// </summary>
            public override bool CanWrite { get { return false; } }

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
            }
        }

        public class PostgreSQLNotifierError : EventArgs
        {
            public Exception ex;

            public PostgreSQLNotifierError(Exception except)
            {
                ex = except;
            }
        }

        public class PostgreSQLNotification : EventArgs
        {
            public DBNotification<T> Notification;

            public PostgreSQLNotification(DBNotification<T> notification)
            {
                Notification = notification;
            }
        }

        public class DBNotification<T>
        {
            public DateTime Timestamp;
            public string operation;
            public string schema;
            public string table;
            public T row;
        }


    }
}
