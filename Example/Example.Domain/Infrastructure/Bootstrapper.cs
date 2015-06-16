namespace Example.Domain
{
    #region << Using >>

    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Web.Mvc;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using FluentValidation;
    using FluentValidation.Mvc;
    using Incoding.Block.IoC;
    using Incoding.Block.Logging;
    using Incoding.CQRS;
    using Incoding.Data;
    using Incoding.EventBroker;
    using Incoding.Extensions;
    using Incoding.MvcContrib;
    using NHibernate.Tool.hbm2ddl;
    using StructureMap.Graph;

    #endregion

    public static class Bootstrapper
    {
        public static void Start()
        {
            //Инициализация LoggingFactory
            LoggingFactory.Instance.Initialize(logging =>
                                                   {
                                                       string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                                                       logging.WithPolicy(policy => policy.For(LogType.Debug)
                                                                                          .Use(FileLogger.WithAtOnceReplace(path,
                                                                                                                            () => "Debug_{0}.txt".F(DateTime.Now.ToString("yyyyMMdd")))));
                                                   });

            //Инициализация IoCFactory
            IoCFactory.Instance.Initialize(init => init.WithProvider(new StructureMapIoCProvider(registry =>
                                                                                                     {
                                                                                                         //Регистрация Dispatcher
                                                                                                         registry.For<IDispatcher>().Use<DefaultDispatcher>();
                                                                                                         //Регистрация Event Broker
                                                                                                         registry.For<IEventBroker>().Use<DefaultEventBroker>();
                                                                                                         registry.For<ITemplateFactory>().Singleton().Use<TemplateHandlebarsFactory>();

                                                                                                         //Настройка FluentlyNhibernate
                                                                                                         var configure = Fluently
                                                                                                                 .Configure()
                                                                                                                 .Database(MsSqlConfiguration.MsSql2008.ConnectionString(ConfigurationManager.ConnectionStrings["Example"].ConnectionString))
                                                                                                                 .Mappings(configuration => configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly))
                                                                                                                 .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(false, true))
                                                                                                                 .CurrentSessionContext<NhibernateSessionContext>(); //Настройка конфигурации базы данных

                                                                                                         registry.For<INhibernateSessionFactory>().Singleton().Use(() => new NhibernateSessionFactory(configure));
                                                                                                         registry.For<IUnitOfWorkFactory>().Use<NhibernateUnitOfWorkFactory>();
                                                                                                         registry.For<IRepository>().Use<NhibernateRepository>();

                                                                                                         //Сканирование текущего Assembly и регистрация всех Validator'ов и Event Subscriber'ов
                                                                                                         registry.Scan(r =>
                                                                                                                           {
                                                                                                                               r.TheCallingAssembly();
                                                                                                                               r.WithDefaultConventions();

                                                                                                                               r.ConnectImplementationsToTypesClosing(typeof(AbstractValidator<>));
                                                                                                                               r.ConnectImplementationsToTypesClosing(typeof(IEventSubscriber<>));
                                                                                                                               r.AddAllTypesOf<ISetUp>();
                                                                                                                           });
                                                                                                     })));

            ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new IncValidatorFactory()));
            FluentValidationModelValidatorProvider.Configure();

            //Запуск всех SetUp на исполнение
            foreach (var setUp in IoCFactory.Instance.ResolveAll<ISetUp>().OrderBy(r => r.GetOrder()))
            {
                setUp.Execute();
            }

            var ajaxDef = JqueryAjaxOptions.Default;
            ajaxDef.Cache = false; //Отключение Ajax cache
        }
    }
}