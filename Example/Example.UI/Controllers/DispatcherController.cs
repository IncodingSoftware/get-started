namespace Example.UI.Controllers
{
    #region << Using >>

    using Example.Domain;
    using Incoding.MvcContrib.MVD;

    #endregion

    public class DispatcherController : DispatcherControllerBase
    {
        #region Constructors

        public DispatcherController()
                : base(typeof(Bootstrapper).Assembly) { }

        #endregion
    }
}