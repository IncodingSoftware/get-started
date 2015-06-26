namespace Example.UnitTests.Operations
{
    #region << Using >>

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(AddCustomerCommand))]
    public class When_add_customer
    {
        #region Constants

        #region Establish value

        static MockMessage<AddCustomerCommand, object> mockCommand;

        #endregion

        #endregion

        #region Fields

        Establish establish = () =>
                                  {
                                      AddCustomerCommand command = Pleasure.Generator.Invent<AddCustomerCommand>();

                                      mockCommand = MockCommand<AddCustomerCommand>
                                              .When(command);
                                  };

        Because of = () => mockCommand.Original.Execute();

        It should_be_saved = () => mockCommand.ShouldBeSave<Customer>(customer => customer.ShouldEqualWeak(mockCommand.Original));

        #endregion
    }
}