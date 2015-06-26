namespace Example.UnitTests.Operations
{
    #region << Using >>

    using System.Collections.Generic;
    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(GetCustomersQuery))]
    public class When_get_customers
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      var query = Pleasure.Generator.Invent<GetCustomersQuery>();
                                      customer = Pleasure.Generator.Invent<Customer>();

                                      mockQuery = MockQuery<GetCustomersQuery, List<GetCustomersQuery.Response>>
                                              .When(query)
                                              .StubQuery(whereSpecification: new CustomerBySearchWhereSpec(query.Search), entities: customer);
                                  };

        Because of = () => mockQuery.Original.Execute();

        It should_be_result = () => mockQuery.ShouldBeIsResult(list => list.ShouldEqualWeakEach(new List<Customer> { customer },
                                                                                                (dsl, i) => dsl.ForwardToValue(r => r.Mark, customer.Mark)
                                                                                                               .ForwardToValue(r => r.FullName, customer.First + customer.Last)
                                                                                                               .ForwardToValue(r => r.Id, customer.Id.ToString())));

        #endregion

        #region Establish value

        static MockMessage<GetCustomersQuery, List<GetCustomersQuery.Response>> mockQuery;

        static Customer customer;

        #endregion
    }
}