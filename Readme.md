<!-- default badges list -->
![](https://img.shields.io/endpoint?url=https://codecentral.devexpress.com/api/v1/VersionRange/245378676/22.2.2%2B)
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T868679)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
<!-- default badges end -->
# WPF Splash Screen - Show a Cancelable Wait Indicator

This example illustrates how to start a complex operation in a background thread and display its progress and status (Loading, Finishing, etc.) in the [Splash Screen](https://docs.devexpress.com/WPF/401685/controls-and-libraries/windows-and-utility-controls/splash-screen-manager). This Splash Screen also contains the **Close** button that allows end users to cancel the operation and close the Splash Screen.

![image](https://user-images.githubusercontent.com/65009440/219656170-9f820806-65e6-43c0-8bab-d357b1593e99.png)

Use [SplashScreenManagerService](https://docs.devexpress.com/WPF/401692/mvvm-framework/services/predefined-set/splashscreenmanagerservice) to operate with this manager in an MVVM way.

## Implementation Details

Add the [SplashScreenManagerService](https://docs.devexpress.com/WPF/DevExpress.Xpf.Core.SplashScreenManagerService) to the MainView. Specify the required Splash Screen UI in the **SplashScreenView** and assign this view to the service's [ViewTemplate](https://docs.devexpress.com/WPF/DevExpress.Mvvm.UI.ViewServiceBase.ViewTemplate): 

```xaml
<dxmvvm:Interaction.Behaviors>
    <dxmvvm:DispatcherService/>
    <dx:SplashScreenManagerService OwnerLockMode="WindowInputOnly"
                                   StartupLocation="CenterOwner">
        <dx:SplashScreenManagerService.ViewTemplate>
            <DataTemplate>
                <Views:SplashScreenView />
            </DataTemplate>
        </dx:SplashScreenManagerService.ViewTemplate>
        <dx:SplashScreenManagerService.SplashScreenWindowStyle>
            <Style TargetType="dx:SplashScreenWindow">
                <Setter Property="AllowAcrylic" Value="True" />
                <Setter Property="AllowsTransparency" Value="True" />
                <Setter Property="Background" Value="#B887A685" />
            </Style>
        </dx:SplashScreenManagerService.SplashScreenWindowStyle>
    </dx:SplashScreenManagerService>
</dxmvvm:Interaction.Behaviors>
```

When the Splash Screen is shown, this SplashScreenView's DataContext contains an instance of the **DXSplashScreenViewModel** (or its descendant) class. You can bind visual elements of the **SplashScreenView** to the `Logo`, `Title`, `Progress`, and `Status` properties from this class. When you change these settings in the **SplashScreenManagerService.ViewModel** object, **SplashScreenView**'s elements reflect these changes.

The [DispatcherService](https://docs.devexpress.com/WPF/113861/mvvm-framework/services/predefined-set/dispatcherservice) allows you to update the Splash Screen's view model properties in the main thread. The executed complex operation does not allow you to update the view model that is created in the main thread.

The main view model is a [ViewModelBase](https://docs.devexpress.com/WPF/17351/mvvm-framework/viewmodels/viewmodelbase) class descendant. Use the approach from the [Services in ViewModelBase descendants](https://docs.devexpress.com/WPF/17446/mvvm-framework/services/services-in-viewmodelbase-descendants) article to get access to the view services:

```cs
public ISplashScreenManagerService SplashScreenManagerService {
    get { return this.GetService<ISplashScreenManagerService>(); }
}

public IDispatcherService DispatcherService {
    get { return this.GetService<IDispatcherService>(); }
}
```

> **NOTE**  
> Refer to the following help topics if you use other view model types:  
> [Services in Generated View Models](https://docs.devexpress.com/WPF/17447/mvvm-framework/services/services-in-generated-view-models)  
> [Services in Custom ViewModels](https://docs.devexpress.com/WPF/17450/mvvm-framework/services/services-in-custom-viewmodels)  

The [BackgroundWorker](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.backgroundworker) class allows you to execute a complex operation in a background thread. Set the `WorkerSupportsCancellation` property to `true` to cancel the operation on demand: 

```cs
BackgroundWorker worker;

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
        worker.DoWork -= Worker_DoWork;
        worker = null;
    });
}
```

When a user clicks the "Start a complex operation" button, initialize properties in the **SplashScreenManagerService.ViewModel** and show the Splash Screen:

```cs
[Command(CanExecuteMethodName = "CanStart")]
public void Start() {
    if(this.SplashScreenManagerService != null) {
        this.SplashScreenManagerService.ViewModel = new DXSplashScreenViewModel();
        this.InitSplashScreenViewModel(this.SplashScreenManagerService.ViewModel);
        this.SplashScreenManagerService.Show();
        this.RunBackgroundWorker();
    }
}
```

In the **InitSplashScreenViewModel** define the **Title**, **SubTitle**, **Progress** and other settings. Set the **Tag** property in the Splash Screen's view model to [DelegateCommand](https://docs.devexpress.com/WPF/17353/mvvm-framework/commands/delegate-commands) that calls the **CancelOperation** method from the main view model:

```cs
void InitSplashScreenViewModel(DXSplashScreenViewModel vm) {
    vm.Title = "SOME BACKGROUND WORK";
    vm.SubTitle = "This can take some time";
    vm.Logo = new Uri("pack://application:,,,/logo.png");
    vm.IsIndeterminate = false;
    vm.Tag = new DelegateCommand(CancelOperation, CanCancelOperation);
}

public bool CanCancelOperation() { return worker != null && worker.IsBusy; }

public void CancelOperation() {
    if(worker != null && worker.IsBusy)
        worker.CancelAsync();
}
```

In the Splash Screen's view, bind the close button's `Command` property to the View Model's **Tag** property:

```xaml
...
<dx:SimpleButton Margin="20"
                 HorizontalAlignment="Right"
                 VerticalAlignment="Top"
                 Command="{Binding Tag}"
                 Glyph="{dx:DXImage GrayScaleImages/Edit/Delete_16x16.png}"
                 ToolTip="Cancel and Close" />
...
```

To update the Splash Screen during a complex operation, set the **Progress** and **State** properties in the **SplashScreenManagerService.ViewModel** object to the required values. To do this in the main thread, use the DispatcherService's **Invoke** method:

```cs
void UpdateSplashScreenContent(int progressValue) {
    var state = string.Empty;
    state = progressValue < 20 ? "Starting..." : progressValue < 70 ? "Loading data.." : "Finishing";
    this.DispatcherService.Invoke(() => {
        this.SplashScreenManagerService.ViewModel.Progress = progressValue;
        this.SplashScreenManagerService.ViewModel.Status = $"({progressValue} %) - {state}";
    });
}
```
