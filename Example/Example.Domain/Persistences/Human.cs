namespace Example.Domain
{
    #region << Using >>

    using System;
    using System.Linq.Expressions;
    using Incoding;
    using Incoding.Data;

    #endregion

    public class Human : IncEntityBase
    {
        #region Properties

        public virtual DateTime Birthday { get; set; }

        public virtual string FirstName { get; set; }

        public new virtual string Id { get; set; }

        public virtual string LastName { get; set; }

        public virtual Sex Sex { get; set; }

        #endregion

        #region Nested Classes

        public class Map : NHibernateEntityMap<Human>
        {
            #region Constructors

            protected Map()
            {
                IdGenerateByGuid(r => r.Id);
                MapEscaping(r => r.FirstName);
                MapEscaping(r => r.LastName);
                MapEscaping(r => r.Birthday);
                MapEscaping(r => r.Sex);
            }

            #endregion
        }

        public abstract class Where
        {
            #region Nested Classes

            public class ByFirstName : Specification<Human>
            {
                #region Properties

                readonly string firstName;

                #endregion

                #region Constructors

                public ByFirstName(string firstName)
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

            public class ByLastName : Specification<Human>
            {
                #region Properties

                readonly string lastName;

                #endregion

                #region Constructors

                public ByLastName(string lastName)
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

            #endregion
        }

        #endregion
    }

    public enum Sex
    {
        Male = 1,

        Female = 2
    }
}