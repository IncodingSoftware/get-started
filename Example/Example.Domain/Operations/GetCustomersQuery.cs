namespace Example.Domain
{
    #region << Using >>

    using System.Collections.Generic;
    using System.Linq;
    using Incoding.CQRS;

    #endregion

    public class GetCustomersQuery : QueryBase<List<GetCustomersQuery.Response>>
    {
        #region Properties

        public string Search { get; set; }

        #endregion

        #region Nested Classes

        public class Response
        {
            #region Properties

            public string Mark { get; set; }

            public string FullName { get; set; }

            public string Id { get; set; }

            #endregion
        }

        #endregion

        protected override List<Response> ExecuteResult()
        {
            return Repository.Query(whereSpecification: new CustomerBySearchWhereSpec(Search))
                             .ToList()
                             .Select(r => new Response
                                              {
                                                      Id = r.Id.ToString(),
                                                      FullName = r.First + r.Last,
                                                      Mark = r.Mark
                                              })
                             .ToList();
        }
    }
}