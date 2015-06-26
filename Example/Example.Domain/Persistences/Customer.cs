namespace Example.Domain
{
    #region << Using >>

    using Incoding.Data;
    using Incoding.Quality;

    #endregion

    public class Customer : IncEntityBase
    {
        #region Properties

        public virtual string First { get; set; }

        [IgnoreCompare("Base field")]
        public virtual string Id { get; set; }

        public virtual string Last { get; set; }

        public virtual string Mark { get; set; }

        #endregion

        #region Nested Classes

        public class Map : NHibernateEntityMap<Customer>
        {
            #region Constructors

            public Map()
            {
                IdGenerateByGuid(r => r.Id);
                MapEscaping(r => r.Last);
                MapEscaping(r => r.First);
                MapEscaping(r => r.Mark);
            }

            #endregion
        }

        #endregion
    }
}