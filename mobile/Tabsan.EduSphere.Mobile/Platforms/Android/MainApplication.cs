using Android.App;
using Android.Runtime;

namespace Tabsan.EduSphere.Mobile;

// Plain http is allowed by Android so the app can reach a server on the institute's own network (http://192.168.1.10:5181,
// the emulator's http://10.0.2.2:5181). The app itself only accepts http for such local addresses and insists on https
// for servers on the internet (Mobile.Core ServerAddress), so passwords never cross the internet unencrypted.
[Application(UsesCleartextTraffic = true)]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
