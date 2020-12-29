using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace ytbrowser {
    class PipeClient {
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        TaskCompletionSource<object> closed = new TaskCompletionSource<object>();
        WeakReference<MainPage> owner;
        NamedPipeClientStream pipeStream;
        AutoResetEvent queueEvent;

        private MainPage Owner => owner?.GetValue();
        private bool Alive => !cancellationToken.IsCancellationRequested;

        private bool Busy = false;

        private StreamWriter Output = null;
        private Queue<string> MessageQueue;

        public PipeClient(MainPage owner) {
            this.owner = new WeakReference<MainPage>(owner);
            MessageQueue = new Queue<string>();
            queueEvent = new AutoResetEvent(false);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            Open();
        }

        public async Task Close() {
            cancellationTokenSource.Cancel();
            pipeStream.Close();
            pipeStream.Dispose();
            await closed.Task;
        }

        private bool DequeueMessage(out string msg) {
            lock (MessageQueue) {
                if (MessageQueue.Count > 0) {
                    msg = MessageQueue.Dequeue();
                    return true;
                }
            }
            msg = null;
            return false;
        }

        private void EnqueueMessage(string msg) {
            lock (MessageQueue) {
                MessageQueue.Enqueue(msg);
            }
            queueEvent.Set();
        }

        private void Open() {
            Task.Run(async () => {
                while (Alive) {
                    try {
                        pipeStream = new NamedPipeClientStream("BooTube.BrowserPipe");
                        await pipeStream.ConnectAsync(cancellationToken);
                        using (Output = new StreamWriter(pipeStream)) {
                            while (Alive) {
                                if (queueEvent.WaitOne(100)) {
                                    string msg;
                                    while (DequeueMessage(out msg)) {
                                        await Output.WriteLineAsync(msg);
                                    }
                                }
                            }
                        }
                    } catch (Exception e) {
                        Output?.Dispose(); Output = null;
                        pipeStream?.Dispose(); pipeStream = null;
                    }
                }
                closed.TrySetResult(null);
            });
        }


        public void Dispose() {
            _ = Close();
        }
    }
}
