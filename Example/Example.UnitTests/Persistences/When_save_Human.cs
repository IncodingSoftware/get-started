namespace Example.UnitTests
{
    #region << Using >>

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(Human))]
    public class When_save_Human : SpecWithPersistenceSpecification<Human>
    {
        #region Fields

        It should_be_verify = () => persistenceSpecification.VerifyMappingAndSchema();

        #endregion
    }
}