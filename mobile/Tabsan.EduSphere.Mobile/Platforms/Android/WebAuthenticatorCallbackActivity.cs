using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Tabsan.EduSphere.Mobile;

/// <summary>
/// Plan M12 — receives the portal's single sign-on redirect ({scheme}://sso?code=…) for WebAuthenticator. The scheme is the
/// white-label AppScheme (default edusphere); the API's Mobile:AppRedirectUri must use the same one.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = WhiteLabelBuild.Scheme, DataHost = "sso")]
public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
