namespace Example.Domain
{
    #region << Using >>

    using Incoding.CQRS;

    #endregion

    public class AddCustomerCommand : CommandBase
    {
        #region Properties

        public string First { get; set; }

        public string Last { get; set; }

        public string Mark { get; set; }

        #endregion

        public override void Execute()
        {
            Repository.Save(new Customer
                                {
                                        First = First,
                                        Last = Last,
                                        Mark = Mark
                                });
        }
    }
}