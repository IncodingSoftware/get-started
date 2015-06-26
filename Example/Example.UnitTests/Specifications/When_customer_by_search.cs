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

    [Subject(typeof(CustomerBySearchWhereSpec))]
    public class When_customer_by_search
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      Func<string, string, Customer> createEntity = (first, last) => Pleasure.MockStrictAsObject<Customer>(mock =>
                                                                                                                                               {
                                                                                                                                                   mock.SetupGet(r => r.First).Returns(first);
                                                                                                                                                   mock.SetupGet(r => r.Last).Returns(last);
                                                                                                                                               });

                                      fakeCollection = Pleasure.ToQueryable(createEntity(Pleasure.Generator.TheSameString(), Pleasure.Generator.TheSameString()),
                                                                            createEntity(Pleasure.Generator.String(), Pleasure.Generator.String()));
                                  };

        Because of = () =>
                         {
                             filterCollection = fakeCollection
                                     .Where(new CustomerBySearchWhereSpec(Pleasure.Generator.TheSameString()).IsSatisfiedBy())
                                     .ToList();
                         };

        It should_be_filter = () =>
                                  {
                                      filterCollection.Count.ShouldEqual(1);
                                      filterCollection[0].First.ShouldContain(Pleasure.Generator.TheSameString());
                                      filterCollection[0].Last.ShouldContain(Pleasure.Generator.TheSameString());
                                  };

        #endregion

        #region Establish value

        static IQueryable<Customer> fakeCollection;

        static List<Customer> filterCollection;

        #endregion
    }
}