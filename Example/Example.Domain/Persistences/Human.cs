namespace Example.Domain
{
    #region << Using >>

    using System;
    using Incoding.Data;
    using Incoding.Quality;

    #endregion

    public class Human : IncEntityBase
    {
        #region Properties

        public virtual DateTime Birthday { get; set; }

        public virtual string FirstName { get; set; }

        [IgnoreCompare("Base field")]
        public virtual string Id { get; set; }

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

        #endregion
    }

    public enum Sex
    {
        Male = 1,

        Female = 2
    }
}