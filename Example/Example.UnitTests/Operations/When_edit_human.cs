namespace Example.UnitTests
{
    #region << Using >>

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(AddOrEditHumanCommand))]
    public class When_edit_human
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      var command = Pleasure.Generator.Invent<AddOrEditHumanCommand>();

                                      human = Pleasure.Generator.Invent<Human>();

                                      mockCommand = MockCommand<AddOrEditHumanCommand>
                                              .When(command)
                                              .StubGetById(command.Id, human);
                                  };

        Because of = () => mockCommand.Original.Execute();

        It should_be_saved = () => mockCommand.ShouldBeSaveOrUpdate<Human>(human => human.ShouldEqualWeak(mockCommand.Original));

        #endregion

        #region Establish value

        static MockMessage<AddOrEditHumanCommand, object> mockCommand;

        static Human human;

        #endregion
    }
}