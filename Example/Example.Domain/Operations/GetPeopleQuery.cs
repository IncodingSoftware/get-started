namespace Example.Domain
{
    #region << Using >>

    using System.Collections.Generic;
    using System.Linq;
    using Incoding.CQRS;
    using Incoding.Extensions;

    #endregion

    public class GetPeopleQuery : QueryBase<List<Human>>
    {
        #region Properties

        public string Keyword { get; set; }

        #endregion

        protected override List<Human> ExecuteResult()
        {
            return Repository.Query(whereSpecification: new HumanByFirstNameWhereSpec(Keyword)
                                            .Or(new HumanByLastNameWhereSpec(Keyword))).ToList();
        }
    }
}