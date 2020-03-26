using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace SplashScreenManagerExample.ViewModels {
    public class MainViewModel : ViewModelBase {
        public ISplashScreenManagerService SplashScreenManagerService
        {
            get { return this.GetService<ISplashScreenManagerService>(); }
        }

        public IDispatcherService DispatcherService
        {
            get { return this.GetService<IDispatcherService>(); }
        }

        BackgroundWorker worker;

        public bool CanStart() { return worker == null || !worker.IsBusy; }

        [Command(CanExecuteMethodName = "CanStart")]
        public void Start() {
            if(this.SplashScreenManagerService != null) {
                this.InitSplashScreenViewModel(this.SplashScreenManagerService.ViewModel);
                this.SplashScreenManagerService.Show();
                this.RunBackgroundWorker();
            }
        }

        void RunBackgroundWorker() {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            int i = -1;
            while(++i < 100) {
                if(worker.CancellationPending) {
                    e.Cancel = true;
                    break;
                }
                UpdateSplashScreenContent(i);
                Thread.Sleep(200);
            }
            this.DispatcherService.Invoke(() => {
                this.SplashScreenManagerService.Close();
            });
            worker = null;
        }

        void UpdateSplashScreenContent(int progressValue) {
            var state = string.Empty;
            state = progressValue < 20 ? "Starting..." : progressValue < 70 ? "Loading data.." : "Finishing";
            this.DispatcherService
                .Invoke(() => {
                this.SplashScreenManagerService.ViewModel.Progress = progressValue;
                this.SplashScreenManagerService.ViewModel.Status = $"({progressValue} %) - {state}";
            });
        }

        void InitSplashScreenViewModel(DXSplashScreenViewModel vm) {
            vm.Title = "SOME BACKGROUND WORK";
            vm.Subtitle = "This can take some time";
            vm.Logo = new Uri("pack://application:,,,/logo.png");
            vm.IsIndeterminate = false;
            vm.Tag = new DelegateCommand(CancelOperation, CanCancelOperation);
        }

        public bool CanCancelOperation() { return worker != null && worker.IsBusy; }

        public void CancelOperation() {
            if(worker != null && worker.IsBusy)
                worker.CancelAsync();
        }
    }
}