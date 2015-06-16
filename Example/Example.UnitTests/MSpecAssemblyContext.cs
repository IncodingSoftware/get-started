namespace Example.UnitTests
{
    #region << Using >>

    using System.Configuration;
    using Example.Domain;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    ////ncrunch: no coverage start	
    public class MSpecAssemblyContext : IAssemblyContext
    {
        #region IAssemblyContext Members

        public void OnAssemblyStart()
        {
            //Настройка подключения к тестовой БД
            var configure = Fluently
                    .Configure()
                    .Database(MsSqlConfiguration.MsSql2008
                                                .ConnectionString(ConfigurationManager.ConnectionStrings["Example_Test"].ConnectionString)
                                                .ShowSql())
                    .Mappings(configuration => configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly));

            PleasureForData.StartNhibernate(configure, true);
        }

        public void OnAssemblyComplete() { }

        #endregion
    }

    ////ncrunch: no coverage end
}