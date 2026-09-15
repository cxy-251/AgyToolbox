using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class SysInfoView : UserControl
{
    private readonly SysInfoService _sysInfoService = new();

    public SysInfoView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadSysInfoAsync();
    }

    private async void BtnRefreshSysInfo_Click(object sender, RoutedEventArgs e)
    {
        await LoadSysInfoAsync();
    }

    private async Task LoadSysInfoAsync()
    {
        var basic = _sysInfoService.GetBasicInfo();
        TxtMachineInfo.Text = $"计算机名: {basic.MachineName} | 操作系统: {basic.OSDescription} ({basic.Architecture})";
        TxtCpuUptime.Text = $"逻辑核心数: {basic.CoreCount} 核心 | 开机运行时长: {basic.Uptime:d'天 'hh'小时 'mm'分'}";

        ListDisks.ItemsSource = _sysInfoService.GetDisks();
        ListNets.ItemsSource = _sysInfoService.GetActiveNetworks();

        ListPings.ItemsSource = new[] { new PingResultItem("正在测速中...", false, 0, "") };
        var pings = await _sysInfoService.TestPingAsync();
        ListPings.ItemsSource = pings;
    }
}
