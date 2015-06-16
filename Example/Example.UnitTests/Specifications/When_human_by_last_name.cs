namespace Example.UnitTests.Specifications
{
    #region << Using >>

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(HumanByLastNameWhereSpec))]
    public class When_human_by_last_name
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      Func<string, Human> createEntity = (lastName) =>
                                                                         Pleasure.MockStrictAsObject<Human>(mock =>
                                                                                                            mock.SetupGet(r => r.LastName)
                                                                                                                .Returns(lastName));

                                      fakeCollection = Pleasure.ToQueryable(createEntity(Pleasure.Generator.TheSameString()),
                                                                            createEntity(Pleasure.Generator.String()));
                                  };

        Because of = () =>
                         {
                             filterCollection = fakeCollection
                                     .Where(new HumanByLastNameWhereSpec(Pleasure.Generator.TheSameString()).IsSatisfiedBy())
                                     .ToList();
                         };

        It should_be_filter = () =>
                                  {
                                      filterCollection.Count.ShouldEqual(1);
                                      filterCollection[0].LastName.ShouldBeTheSameString();
                                  };

        #endregion

        #region Establish value

        static IQueryable<Human> fakeCollection;

        static List<Human> filterCollection;

        #endregion
    }
}