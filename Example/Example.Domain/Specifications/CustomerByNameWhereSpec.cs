namespace Example.Domain
{
    #region << Using >>

    using System;
    using System.Linq.Expressions;
    using Incoding;

    #endregion

    public class CustomerBySearchWhereSpec : Specification<Customer>
    {
        #region Fields

        readonly string keyword;


        #endregion

        #region Constructors

        public CustomerBySearchWhereSpec(string keyword)
        {
            this.keyword = keyword;
        }

        #endregion

        public override Expression<Func<Customer, bool>> IsSatisfiedBy()
        {
            return customer => customer.First.Contains(this.keyword) ||
                               customer.Last.Contains(this.keyword);
        }
    }
}