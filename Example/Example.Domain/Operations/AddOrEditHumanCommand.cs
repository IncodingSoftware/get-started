namespace Example.Domain
{
    #region << Using >>

    using System;
    using FluentValidation;
    using Incoding.CQRS;
    using Incoding.Extensions;

    #endregion

    public class AddOrEditHumanCommand : CommandBase
    {
        #region Properties

        public DateTime BirthDay { get; set; }

        public string FirstName { get; set; }

        public string Id { get; set; }

        public string LastName { get; set; }

        public Sex Sex { get; set; }

        #endregion

        #region Nested Classes

        public class Validator : AbstractValidator<AddOrEditHumanCommand>
        {
            #region Constructors

            public Validator()
            {
                RuleFor(r => r.FirstName).NotEmpty();
                RuleFor(r => r.LastName).NotEmpty();
            }

            #endregion
        }

        #endregion

        public override void Execute()
        {
            var human = Repository.GetById<Human>(Id) ?? new Human();

            human.FirstName = FirstName;
            human.LastName = LastName;
            human.Birthday = BirthDay;
            human.Sex = Sex;

            Repository.SaveOrUpdate(human);
        }
    }
}