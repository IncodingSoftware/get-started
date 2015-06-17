namespace Example.Domain
{
    #region << Using >>

    using System.Collections.Generic;
    using System.Linq;
    using Incoding.CQRS;
    using Incoding.Extensions;

    #endregion

    public class GetPeopleQuery : QueryBase<List<GetPeopleQuery.Response>>
    {
        #region Properties

        public string Keyword { get; set; }

        #endregion

        #region Nested Classes

        public class Response
        {
            #region Properties

            public string Birthday { get; set; }

            public string FirstName { get; set; }

            public string Id { get; set; }

            public string LastName { get; set; }

            public string Sex { get; set; }

            #endregion
        }

        #endregion

        protected override List<Response> ExecuteResult()
        {
            return Repository.Query(whereSpecification: new HumanByFirstNameWhereSpec(Keyword)
                                            .Or(new HumanByLastNameWhereSpec(Keyword)))
                             .Select(human => new Response
                                                  {
                                                          Id = human.Id,
                                                          Birthday = human.Birthday.ToShortDateString(),
                                                          FirstName = human.FirstName,
                                                          LastName = human.LastName,
                                                          Sex = human.Sex.ToString()
                                                  }).ToList();
        }
    }
}