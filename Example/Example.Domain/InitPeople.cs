namespace Example.Domain
{
    #region << Using >>

    using System;
    using Incoding.Block.IoC;
    using Incoding.CQRS;
    using NHibernate.Util;

    #endregion

    public class InitPeople : ISetUp
    {
        public void Dispose() { }

        public int GetOrder()
        {
            return 0;
        }

        public void Execute()
        {
            var dispatcher = IoCFactory.Instance.TryResolve<IDispatcher>();

            if (dispatcher.Query(new GetEntitiesQuery<Human>()).Any())
                return;

            dispatcher.Push(new AddOrEditHumanCommand
                                {
                                        FirstName = "Hellen",
                                        LastName = "Jonson",
                                        BirthDay = Convert.ToDateTime("06/05/1985"),
                                        Sex = Sex.Female
                                });
            dispatcher.Push(new AddOrEditHumanCommand
                                {
                                        FirstName = "John",
                                        LastName = "Carlson",
                                        BirthDay = Convert.ToDateTime("06/07/1985"),
                                        Sex = Sex.Male
                                });
        }
    }
}