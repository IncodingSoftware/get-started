#region << Using >>

using Example.UI.App_Start;
using WebActivator;

#endregion

[assembly: PreApplicationStartMethod(
        typeof(IncodingStart), "PreStart")]

namespace Example.UI.App_Start
{
    #region << Using >>

    using Example.Domain;
    using Example.UI.Controllers;

    #endregion

    public static class IncodingStart
    {
        public static void PreStart()
        {
            Bootstrapper.Start();
            new DispatcherController(); // init routes
        }
    }
}