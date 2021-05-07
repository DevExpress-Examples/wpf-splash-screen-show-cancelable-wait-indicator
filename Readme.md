# How to show a cancelable wait indicator (Splash Screen) and update its content during a complex background operation

Starting with v20.1, we provide **SplashScreenManager** with flexible options to design a custom Splash Screen view and update its content. Use **SplashScreenManagerService** to operate with this manager in an MVVM way. For more information about Services, refer to the [Services](https://docs.devexpress.com/WPF/17414/mvvm-framework/services) help topic.

This example illustrates how to start a complex operation in a background thread and display its progress and status (Loading, Finishing, etc.) in the Splash Screen. This Splash Screen also contains the **Close** button that allows end users to cancel the operation and close the Splash Screen.

We add **SplashScreenManagerService** to MainView and use a custom UserControl - **SplashScreenView** - with the required Splash Screen UI in this service's **ViewTemplate**: 

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

When the Splash Screen is shown, this SplashScreenView's DataContext will contain an instance of the **DXSplashScreenViewModel** (or its descendant) class. That is why we can bind visual elements of this view to the Logo, Title, Progress, Status, etc., properties from this class. When we change these settings in the **SplashScreenManagerService.ViewModel** object at the view model level, **SplashScreenView**'s elements will reflect these changes.

We also add [DispatcherService](https://docs.devexpress.com/WPF/113861/mvvm-framework/services/predefined-set/dispatcherservice) to MainView so that we can update properties in the Splash Screen's view model in the main thread. This is required because we will execute a complex operation in a separate thread, which does not allow us to update the view model that is created in the main thread.

The main view model is a [ViewModelBase](https://docs.devexpress.com/WPF/17351/mvvm-framework/viewmodels/viewmodelbase) class descendant. We need to use the approach from the [Services in ViewModelBase descendants](https://docs.devexpress.com/WPF/17446/mvvm-framework/services/services-in-viewmodelbase-descendants) article to get access to the services that we added to our main view:

```cs
public ISplashScreenManagerService SplashScreenManagerService
{
    get { return this.GetService<ISplashScreenManagerService>(); }
}

public IDispatcherService DispatcherService
{
    get { return this.GetService<IDispatcherService>(); }
}

```

> **NOTE**  
> Refer to these links if you use other view model types:  
> [Services in POCO objects](https://docs.devexpress.com/WPF/17447/mvvm-framework/services/services-in-poco-objects)  
> [Services in custom ViewModels](https://docs.devexpress.com/WPF/17450/mvvm-framework/services/services-in-custom-viewmodels)  

We execute a complex operation in a background thread with the help of the [BackgroundWorker](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.backgroundworker?view=netframework-4.8) class. We enable its **WorkerSupportsCancellation** option so that we can cancel its execution when necessary: 

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

When an end user clicks the "Start a complex operation" button, we initialize properties in **SplashScreenManagerService.ViewModel** and show the Splash Screen:

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

In the **InitSplashScreenViewModel** we define the corresponding **Title**, **SubTitle**, **Progress** and other settings. Additionally, we set the **Tag** property in this Splash Screen's view model to [DelegateCommand](https://docs.devexpress.com/WPF/17353/mvvm-framework/commands/delegate-commands) that will call the **CancelOperation** method from the main view model:

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

With this approach, we can use this **Tag** property at the level of the Splash Screen's view to allow end users to cancel the background operation:


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

To update the Splash Screen during a complex operation, we set the **Progress** and **State** properties in the **SplashScreenManagerService.ViewModel** object to the required values. To do this in the main thread, we use DispatcherService's **Invoke** method:

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
