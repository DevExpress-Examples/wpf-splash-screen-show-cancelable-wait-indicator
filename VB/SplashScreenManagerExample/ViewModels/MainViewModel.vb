Imports System
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Threading
Imports DevExpress.Mvvm
Imports DevExpress.Mvvm.DataAnnotations

Namespace SplashScreenManagerExample.ViewModels
	Public Class MainViewModel
		Inherits ViewModelBase

		Public ReadOnly Property SplashScreenManagerService() As ISplashScreenManagerService
			Get
				Return Me.GetService(Of ISplashScreenManagerService)()
			End Get
		End Property

		Public ReadOnly Property DispatcherService() As IDispatcherService
			Get
				Return Me.GetService(Of IDispatcherService)()
			End Get
		End Property

		Private worker As BackgroundWorker

		Public Function CanStart() As Boolean
			Return worker Is Nothing OrElse Not worker.IsBusy
		End Function

		<Command(CanExecuteMethodName := "CanStart")>
		Public Sub Start()
			If Me.SplashScreenManagerService IsNot Nothing Then
				Me.SplashScreenManagerService.ViewModel = New DXSplashScreenViewModel()
				Me.InitSplashScreenViewModel(Me.SplashScreenManagerService.ViewModel)
				Me.SplashScreenManagerService.Show()
				Me.RunBackgroundWorker()
			End If
		End Sub

		Private Sub RunBackgroundWorker()
			worker = New BackgroundWorker()
			AddHandler worker.DoWork, AddressOf Worker_DoWork
			worker.WorkerSupportsCancellation = True
			worker.RunWorkerAsync()
		End Sub

		Private Sub Worker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
			Dim i As Integer = -1
			i += 1
'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
'ORIGINAL LINE: while(++i < 100)
			Do While i < 100
				If worker.CancellationPending Then
					e.Cancel = True
					Exit Do
				End If
				UpdateSplashScreenContent(i)
				Thread.Sleep(200)
				i += 1
			Loop
			Me.DispatcherService.Invoke(Sub()
				Me.SplashScreenManagerService.Close()
				RemoveHandler worker.DoWork, AddressOf Worker_DoWork
				worker = Nothing
			End Sub)
		End Sub

		Private Sub UpdateSplashScreenContent(ByVal progressValue As Integer)
			Dim state = String.Empty
			state = If(progressValue < 20, "Starting...", If(progressValue < 70, "Loading data..", "Finishing"))
			Me.DispatcherService.Invoke(Sub()
				Me.SplashScreenManagerService.ViewModel.Progress = progressValue
				Me.SplashScreenManagerService.ViewModel.Status = $"({progressValue} %) - {state}"
			End Sub)
		End Sub

		Private Sub InitSplashScreenViewModel(ByVal vm As DXSplashScreenViewModel)
			vm.Title = "SOME BACKGROUND WORK"
			vm.Subtitle = "This can take some time"
			vm.Logo = New Uri("pack://application:,,,/logo.png")
			vm.IsIndeterminate = False
			vm.Tag = New DelegateCommand(AddressOf CancelOperation, AddressOf CanCancelOperation)
		End Sub

		Public Function CanCancelOperation() As Boolean
			Return worker IsNot Nothing AndAlso worker.IsBusy
		End Function

		Public Sub CancelOperation()
			If worker IsNot Nothing AndAlso worker.IsBusy Then
				worker.CancelAsync()
			End If
		End Sub
	End Class
End Namespace