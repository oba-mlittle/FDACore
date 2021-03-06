﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FDAScripter
{
    public partial class frmCompileResult : Form
    {
        public frmCompileResult(ImmutableArray<Diagnostic> diags)
        {
            InitializeComponent();

            BindingList<ErrorItem> errors = new BindingList<ErrorItem>();
            
            foreach (Diagnostic diag in diags)
                errors.Add(new frmCompileResult.ErrorItem(diag));

            dgvDiagnostics.AutoGenerateColumns = true;
            dgvDiagnostics.DataSource = errors;
        }

        public class ErrorItem
        {
            public string ID { get; }
            public string Description { get; }
            public string Location{ get; }

            public ErrorItem(Diagnostic ErrorItemSource)
            {
                ID = ErrorItemSource.Id;
                Description = ErrorItemSource.GetMessage();
                Location = "Line " + ErrorItemSource.Location.GetLineSpan().StartLinePosition;
            }
        }

    }
}
