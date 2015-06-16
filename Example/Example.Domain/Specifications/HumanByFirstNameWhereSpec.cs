namespace Example.Domain
{
    #region << Using >>

    using System;
    using System.Linq.Expressions;
    using Incoding;

    #endregion

    public class HumanByFirstNameWhereSpec : Specification<Human>
    {
        #region Fields

        readonly string firstName;

        #endregion

        #region Constructors

        public HumanByFirstNameWhereSpec(string firstName)
        {
            this.firstName = firstName;
        }

        #endregion

        public override Expression<Func<Human, bool>> IsSatisfiedBy()
        {
            if (string.IsNullOrEmpty(this.firstName))
                return null;

            return human => human.FirstName.ToLower().Contains(this.firstName.ToLower());
        }
    }
}