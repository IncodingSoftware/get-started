namespace Example.UnitTests.Operations
{
    #region << Using >>

    using System.Collections.Generic;
    using System.Linq;
    using Example.Domain;
    using Incoding.Extensions;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(GetPeopleQuery))]
    public class When_get_people_query
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      var query = Pleasure.Generator.Invent<GetPeopleQuery>();
                                      human = Pleasure.Generator.Invent<Human>();
                                      expected = new[] { human }.ToList();

                                      mockQuery = MockQuery<GetPeopleQuery, List<Human>>
                                              .When(query)
                                              .StubQuery(whereSpecification: new HumanByFirstNameWhereSpec(query.Keyword)
                                                                 .Or(new HumanByLastNameWhereSpec(query.Keyword)), entities: human);
                                  };

        Because of = () => mockQuery.Original.Execute();

        It should_be_result = () => mockQuery.ShouldBeIsResult(expected);

        #endregion

        #region Establish value

        static MockMessage<GetPeopleQuery, List<Human>> mockQuery;

        static List<Human> expected;

        static Human human;

        #endregion
    }
}