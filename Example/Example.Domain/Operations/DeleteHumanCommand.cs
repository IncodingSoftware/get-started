namespace Example.Domain
{
    #region << Using >>

    using Incoding.CQRS;

    #endregion

    public class DeleteHumanCommand : CommandBase
    {
        #region Properties

        public string HumanId { get; set; }

        #endregion

        public override void Execute()
        {
            Repository.Delete<Human>(HumanId);
        }
    }
}