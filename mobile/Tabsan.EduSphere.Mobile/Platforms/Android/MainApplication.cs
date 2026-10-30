using Android.App;
using Android.Runtime;

namespace Tabsan.EduSphere.Mobile;

// Debug builds may talk to a development API over http (the emulator reaches the PC as http://10.0.2.2:5181);
// Release builds keep Android's default of https only.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
