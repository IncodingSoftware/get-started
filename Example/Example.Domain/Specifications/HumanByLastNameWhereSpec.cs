namespace Example.Domain
{
    #region << Using >>

    using System;
    using System.Linq.Expressions;
    using Incoding;

    #endregion

    public class HumanByLastNameWhereSpec : Specification<Human>
    {
        #region Fields

        readonly string lastName;

        #endregion

        #region Constructors

        public HumanByLastNameWhereSpec(string lastName)
        {
            this.lastName = lastName;
        }

        #endregion

        public override Expression<Func<Human, bool>> IsSatisfiedBy()
        {
            if (string.IsNullOrEmpty(this.lastName))
                return null;

            return human => human.LastName.ToLower().Contains(this.lastName.ToLower());
        }
    }
}