﻿namespace Example.UnitTests.Operations
{
    #region << Using >>

    using Example.Domain;
    using Incoding.Extensions;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(AddOrEditHumanCommand))]
    public class When_add_human
    {
        #region Fields

        Establish establish = () =>
                                  {
                                      var command = Pleasure.Generator.Invent<AddOrEditHumanCommand>();

                                      human = new Human
                                                  {
                                                          Birthday = command.BirthDay.ToShortDateString(),
                                                          FirstName = command.FirstName,
                                                          LastName = command.LastName,
                                                          Sex = command.Sex.ToLocalization()
                                                  };

                                      mockCommand = MockCommand<AddOrEditHumanCommand>
                                              .When(command)
                                              .StubGetById<Human>(command.Id, null);
                                  };

        Because of = () => mockCommand.Original.Execute();

        It should_be_save = () => mockCommand.ShouldBeSaveOrUpdate<Human>(entity =>
                                                                          entity.ShouldEqualWeak(mockCommand.Original,
                                                                                                 dsl =>
                                                                                                 dsl.ForwardToValue(r => r.FirstName, human.FirstName)
                                                                                                    .ForwardToValue(r => r.LastName, human.LastName)
                                                                                                    .ForwardToValue(r => r.Birthday, human.Birthday)
                                                                                                    .ForwardToValue(r => r.Sex, human.Sex)
                                                                                                    .ForwardToValue(r => r.Id, null)));

        #endregion

        #region Establish value

        static MockMessage<AddOrEditHumanCommand, object> mockCommand;

        static Human human;

        #endregion
    }
}