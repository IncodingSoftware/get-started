namespace Example.UnitTests.Persistences
{
    #region << Using >>

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(Customer))]
    public class When_save_Customer : SpecWithPersistenceSpecification<Customer>
    {
        #region Fields

        It should_be_verify = () => persistenceSpecification.VerifyMappingAndSchema();

        #endregion
    }
}