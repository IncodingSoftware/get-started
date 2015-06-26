namespace Example.UnitTests.Operations
{
    #region << Using >>

    using System.Collections.Generic;
    using Example.Domain;
    using Incoding.Extensions;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(GetPeopleQuery))]
    public class When_get_people
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      var query = Pleasure.Generator.Invent<GetPeopleQuery>();
                                      human = Pleasure.Generator.Invent<Human>();

                                      mockQuery = MockQuery<GetPeopleQuery, List<GetPeopleQuery.Response>>
                                              .When(query)
                                              .StubQuery(whereSpecification: new HumanByFirstNameWhereSpec(query.Keyword)
                                                                 .Or(new HumanByLastNameWhereSpec(query.Keyword)),
                                                         entities: human);
                                  };

        Because of = () => mockQuery.Original.Execute();

        It should_be_result = () => mockQuery.ShouldBeIsResult(list => list.ShouldEqualWeakEach(new List<Human>() { human },
                                                                                                (dsl, i) => dsl.ForwardToValue(r => r.Birthday, human.Birthday.ToShortDateString())
                                                                                                               .ForwardToValue(r => r.Sex, human.Sex.ToString())
                                                                               ));

        #endregion

        #region Establish value

        static MockMessage<GetPeopleQuery, List<GetPeopleQuery.Response>> mockQuery;

        static Human human;

        #endregion
    }
}