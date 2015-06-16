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

    [Subject(typeof(HumanByFirstNameWhereSpec))]
    public class When_human_by_first_name
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      Func<string, Human> createEntity = (firstName) =>
                                                                         Pleasure.MockStrictAsObject<Human>(mock =>
                                                                                                            mock.SetupGet(r => r.FirstName)
                                                                                                                .Returns(firstName));

                                      fakeCollection = Pleasure.ToQueryable(createEntity(Pleasure.Generator.TheSameString()),
                                                                            createEntity(Pleasure.Generator.String()));
                                  };

        Because of = () =>
                         {
                             filterCollection = fakeCollection
                                     .Where(new HumanByFirstNameWhereSpec(Pleasure.Generator.TheSameString()).IsSatisfiedBy())
                                     .ToList();
                         };

        It should_be_filter = () =>
                                  {
                                      filterCollection.Count.ShouldEqual(1);
                                      filterCollection[0].FirstName.ShouldBeTheSameString();
                                  };

        #endregion

        #region Establish value

        static IQueryable<Human> fakeCollection;

        static List<Human> filterCollection;

        #endregion
    }
}