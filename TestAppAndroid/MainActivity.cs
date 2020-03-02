using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Threading.Tasks;
using LogicReinc.WebApp.Android;

namespace TestAppAndroid
{
    //Can be bypassed using startActivity directly
    //but as launch activity you will need to do something like this
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : WebAppActivity<AndroidVueWindow> { }
}