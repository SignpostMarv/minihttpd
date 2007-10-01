using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using MiniHttpd;
using MiniHttpd.FileSystem;

namespace MiniHttpdApp
{
    /// <summary>
    /// Summary description for TransferMonitor.
    /// </summary>
    public class TransferMonitorForm : Form
    {
        private readonly Hashtable _clients = new Hashtable();
        private ColumnHeader _clientColumn;
        private IContainer _components;
        private ColumnHeader _lastRequestColumn;
        private ColumnHeader _progressColumn;
        private ColumnHeader _responseColumn;
        private HttpWebServer _server;
        private ColumnHeader _speedColumn;
        private ListView _transferView;
        private Timer _updateTimer;

        public TransferMonitorForm(HttpWebServer server)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            _server = server;
            server.ClientConnected += server_ClientConnected;
            server.ClientDisconnected += server_ClientDisconnected;
            server.RequestReceived += server_RequestReceived;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_components != null)
                    _components.Dispose();
            }
            base.Dispose(disposing);
        }

        private static string CalculateReadableByteLength(long bytes)
        {
            if (bytes < 1024 * 0.96)
                return bytes.ToString(CultureInfo.InvariantCulture) + " bytes";
            if (bytes < 1024 * 1024 * 0.96)
                return (bytes / (decimal) 1024).ToString("0.00", CultureInfo.InvariantCulture) + " KB";
            if (bytes < 1024 * 1024 * 1024 * 0.96)
                return (bytes / (decimal) (1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + " MB";
            if (bytes < 1024 * 1024 * 1024 * (decimal) 1024 * (decimal) 0.5)
                return (bytes / (decimal) (1024 * 1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + " GB";

            return
                (bytes / (decimal) (1024 * 1024 * 1024 * (decimal) 1024)).ToString("0.00", CultureInfo.InvariantCulture) +
                " TB";
        }

        private static string[] EmptyStrings(int count)
        {
            string[] strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
                strings[i] = "";
            return strings;
        }

        private void server_ClientConnected(object sender, ClientEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new HttpServer.ClientEventHandler(server_ClientConnected), new object[] {sender, e});
                return;
            }

            //Why isn't there a ListViewItem(int columns) constructor? -_-
            ListViewItem item = new ListViewItem(EmptyStrings(_transferView.Columns.Count));
            item.SubItems[_clientColumn.Index].Text = e.HttpClient.RemoteAddress;

            _clients.Add(e.HttpClient, item);

            _transferView.Items.Add(item);
        }

        private void server_ClientDisconnected(object sender, ClientEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new HttpServer.ClientEventHandler(server_ClientDisconnected), new object[] {sender, e});
                return;
            }

            _transferView.Items.Remove(_clients[e.HttpClient] as ListViewItem);
            _clients.Remove(e.HttpClient);
        }

        private void server_RequestReceived(object sender, RequestEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new HttpServer.RequestEventHandler(server_RequestReceived), new object[] {sender, e});
                return;
            }

            e.Request.Response.SendingResponse += Response_SendingResponse;
            e.Request.Response.SentResponse += Response_SentResponse;
        }

        private void Response_SendingResponse(object sender, ResponseEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new HttpResponse.ResponseEventHandler(Response_SendingResponse), new object[] {sender, e});
                return;
            }

            ListViewItem item = _clients[e.HttpClient] as ListViewItem;

            if (item == null)
                return;

            if (e.Response.Request.Uri != null)
            {
                item.SubItems[_lastRequestColumn.Index].Text = UrlEncoding.Decode(e.Response.Request.Uri.PathAndQuery);
                item.SubItems[_responseColumn.Index].Text = StatusCodes.GetDescription(e.Response.ResponseCode);
            }

            item.Tag = new TransferTag(e.Response);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            _transferView.BeginUpdate();

            foreach (DictionaryEntry de in _clients)
            {
                ListViewItem item = de.Value as ListViewItem;
                if (item == null || item.Tag == null)
                    continue;

                TransferTag tag = item.Tag as TransferTag;
                if (tag == null)
                    continue;

                HttpResponse response = tag.response;

                if (response.ContentLength >= 0)
                {
                    item.SubItems[_progressColumn.Index].Text = string.Format("{0} of {1} ({2}%)",
                                                                              CalculateReadableByteLength(
                                                                                  response.BytesSent),
                                                                              CalculateReadableByteLength(
                                                                                  response.ContentLength),
                                                                              (response.BytesSent /
                                                                               (double) response.ContentLength * 100d).
                                                                                  ToString("0.00"));
                }
                else
                    item.SubItems[_progressColumn.Index].Text = CalculateReadableByteLength(response.BytesSent);

                item.SubItems[_speedColumn.Index].Text = string.Format("{0}/s",
                                                                       CalculateReadableByteLength(response.BytesSent -
                                                                                                   tag.lastBytesSent));

                tag.lastBytesSent = response.BytesSent;
            }

            _transferView.EndUpdate();
        }

        private void Response_SentResponse(object sender, ResponseEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new HttpResponse.ResponseEventHandler(Response_SentResponse), new object[] {sender, e});
                return;
            }
            ListViewItem item = _clients[e.HttpClient] as ListViewItem;
            if (item == null)
                return;

            item.Tag = null;

            item.SubItems[_progressColumn.Index].Text = string.Format("Done ({0})",
                                                                      CalculateReadableByteLength(e.Response.BytesSent));
        }

        private void TransferMonitorForm_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._components = new System.ComponentModel.Container();
            this._transferView = new ListView();
            this._clientColumn = new System.Windows.Forms.ColumnHeader();
            this._lastRequestColumn = new System.Windows.Forms.ColumnHeader();
            this._progressColumn = new System.Windows.Forms.ColumnHeader();
            this._speedColumn = new System.Windows.Forms.ColumnHeader();
            this._updateTimer = new System.Windows.Forms.Timer(this._components);
            this._responseColumn = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // transferView
            // 
            this._transferView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
                                                    {
                                                        this._clientColumn,
                                                        this._lastRequestColumn,
                                                        this._progressColumn,
                                                        this._speedColumn,
                                                        this._responseColumn
                                                    });
            this._transferView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._transferView.Location = new System.Drawing.Point(0, 0);
            this._transferView.Name = "_transferView";
            this._transferView.Size = new System.Drawing.Size(480, 260);
            this._transferView.TabIndex = 0;
            this._transferView.View = System.Windows.Forms.View.Details;
            // 
            // clientColumn
            // 
            this._clientColumn.Text = "Client";
            this._clientColumn.Width = 80;
            // 
            // lastRequestColumn
            // 
            this._lastRequestColumn.Text = "Last Request";
            this._lastRequestColumn.Width = 180;
            // 
            // progressColumn
            // 
            this._progressColumn.Text = "Progress";
            this._progressColumn.Width = 92;
            // 
            // speedColumn
            // 
            this._speedColumn.Text = "Speed";
            // 
            // updateTimer
            // 
            this._updateTimer.Enabled = true;
            this._updateTimer.Interval = 1000;
            this._updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // responseColumn
            // 
            this._responseColumn.Text = "Response";
            this._responseColumn.Width = 44;
            // 
            // TransferMonitorForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(480, 260);
            this.Controls.Add(this._transferView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TransferMonitorForm";
            this.Text = "Transfer Monitor";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.TransferMonitorForm_Closing);
            this.ResumeLayout(false);
        }

        #endregion

        #region Nested type: TransferTag

        private class TransferTag
        {
            public long lastBytesSent;
            public HttpResponse response;

            public TransferTag(HttpResponse response)
            {
                this.response = response;
            }
        }

        #endregion
    }
}