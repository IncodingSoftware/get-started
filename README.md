# get-started
<p style="text-align: justify;"><em><strong>disclaimer:</strong> данная статья является пошаговым руководством, которое поможет ознакомиться с основными возможностями <strong>Incoding Framework</strong>. Результатом следования данному руководству будет приложение, реализующее работу с БД (CRUD + data filters) и полностью покрытое юнит-тестами.</em></p>

<h1 style="text-align: justify;">Часть 0. Введение.</h1>
<p style="text-align: justify;">Для начала приведем краткое описание фреймворка<strong>. </strong><strong>Incoding</strong> <strong>Framework</strong> состоит из трех пакетов: Incoding framework – back-end проекта, Incoding Meta Language – front-end проекта и Incoding tests helpers – юнит-тесты для back-end’а. Эти пакеты устанавливаются независимо друг от друга, что позволяет интегрировать фреймворк в проект частями: Вы можете подключить только клиентскую или только серверную часть (тесты очень сильно связаны с серверной частью, поэтому их можно позиционировать как дополнение).</p>
<p style="text-align: justify;">В проектах, написанных на <strong>Incoding Framework</strong>,<strong> </strong>в качестве серверной архитектуры используется <a title="Martin Fowler: CQRS" href="http://martinfowler.com/bliki/CQRS.html" target="_blank">CQRS</a>. В качестве основного инструмента построения клиентской части используется <a title="Habrahabr: Incoding rapid development framework" href="http://habrahabr.ru/post/209734/" target="_blank">Incoding Meta Language</a>. В целом <strong>Incoding Framework </strong>покрывает весь цикл разработки приложения.</p>
<p style="text-align: justify;">Типичный solution, созданный с помощью <strong>Incoding Framework, </strong>имеет 3 проекта:</p>

<ol style="text-align: justify;">
	<li style="text-align: justify;"><b>Domain (<em>class library) </em></b><em>- </em>отвечает за бизнес-логику и работу с базой данных.</li>
	<li style="text-align: justify;"><b>UI (<em>ASP.NET MVC project</em>)<i> </i></b><i>- </i>клиентская часть, основанная на ASP.NET MVC.</li>
	<li style="text-align: justify;"><strong>UnitTests (<em>class library</em>) </strong>- юнит-тесты для Domain.</li>
</ol>
<h3 style="text-align: justify;">Domain</h3>
<p style="text-align: justify;">После установки пакета <a title="Nuget: Incoding framework" href="https://www.nuget.org/packages/Incoding.Framework/" target="_blank">Incoding framework</a> через Nuget в проект помимо необходимых dll устанавливается файл Bootstrapper.cs. Основная задача этого файла - инициализация приложения: инициализация логирования, регистрация IoC, установка настроек Ajax-запросов и пр. В качестве IoC framework по умолчанию устанавливается <a title="StructureMap docs" href="http://docs.structuremap.net/">StructureMap</a>, однако есть провайдер для Ninject, а также есть возможность написания своих реализаций.</p>

<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

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
            LoggingFactory.Instance.Initialize(logging =&gt;
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                    logging.WithPolicy(policy =&gt; policy.For(LogType.Debug)
                                                        .Use(FileLogger.WithAtOnceReplace(path,
                                                                                        () =&gt; "Debug_{0}.txt".F(DateTime.Now.ToString("yyyyMMdd")))));
                });

            //Инициализация IoCFactory
            IoCFactory.Instance.Initialize(init =&gt; init.WithProvider(new StructureMapIoCProvider(registry =&gt;
                {
                    //Регистрация Dispatcher
                    registry.For&lt;IDispatcher&gt;().Use&lt;DefaultDispatcher&gt;();
                    //Регистрация Event Broker
                    registry.For&lt;IEventBroker&gt;().Use&lt;DefaultEventBroker&gt;();
                    registry.For&lt;ITemplateFactory&gt;().Singleton().Use&lt;TemplateHandlebarsFactory&gt;();

                    //Настройка FluentlyNhibernate
                    var configure = Fluently
                            .Configure()
                            .Database(MsSqlConfiguration.MsSql2008.ConnectionString(ConfigurationManager.ConnectionStrings["Example"].ConnectionString))
                            .Mappings(configuration =&gt; configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly))
                            .ExposeConfiguration(cfg =&gt; new SchemaUpdate(cfg).Execute(false, true))
                            .CurrentSessionContext&lt;NhibernateSessionContext&gt;(); //Настройка конфигурации базы данных

                    registry.For&lt;INhibernateSessionFactory&gt;().Singleton().Use(() =&gt; new NhibernateSessionFactory(configure));
                    registry.For&lt;IUnitOfWorkFactory&gt;().Use&lt;NhibernateUnitOfWorkFactory&gt;();
                    registry.For&lt;IRepository&gt;().Use&lt;NhibernateRepository&gt;();

                    //Сканирование текущего Assembly и регистрация всех Validator'ов и Event Subscriber'ов
                    registry.Scan(r =&gt;
                                    {
                                        r.TheCallingAssembly();
                                        r.WithDefaultConventions();

                                        r.ConnectImplementationsToTypesClosing(typeof(AbstractValidator&lt;&gt;));
                                        r.ConnectImplementationsToTypesClosing(typeof(IEventSubscriber&lt;&gt;));
                                        r.AddAllTypesOf&lt;ISetUp&gt;();
                                    });
                })));

            ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new IncValidatorFactory()));
            FluentValidationModelValidatorProvider.Configure();

            //Запуск всех SetUp на исполнение
            foreach (var setUp in IoCFactory.Instance.ResolveAll&lt;ISetUp&gt;().OrderBy(r =&gt; r.GetOrder()))
            {
                setUp.Execute();
            }

            var ajaxDef = JqueryAjaxOptions.Default;
            ajaxDef.Cache = false; //Отключение Ajax cache
        }
    }
}</pre>
<p style="text-align: justify;">Далее в <strong>Domain </strong>дописываются команды (Command) и запросы (Query), которые выполняют операции с базой данных либо какие-то другие действия, связанные с бизнес-логикой приложения.</p>

<h3 style="text-align: justify;">UI</h3>
<p style="text-align: justify;">Пакет <a title="Nuget: Incoding Meta Language" href="https://www.nuget.org/packages/Incoding.MetaLanguage/">Incoding Meta Language</a> при установке добавляет в проект необходимые dll, а также файлы IncodingStart.cs и DispatcherController.cs (часть <a title="Habrahabr: Model View Dispatcher (cqrs over mvc)" href="http://habrahabr.ru/post/221585/">MVD</a>) необходимые для работы с Domain.</p>

<pre class="lang:c# decode:true">public static class IncodingStart
{
    public static void PreStart()
    {
        Bootstrapper.Start();
        new DispatcherController(); // init routes
    }
}</pre>
<pre class="lang:c# decode:true">public class DispatcherController : DispatcherControllerBase
{
    #region Constructors

    public DispatcherController()
            : base(typeof(Bootstrapper).Assembly) { }

    #endregion
}</pre>
<p style="text-align: justify;">После установки в <strong>UI</strong> дописывается клиентская логика с использованием <a title="Habrahabr: Incoding rapid development framework" href="http://habrahabr.ru/post/209734/" target="_blank">IML</a>.</p>

<h3>UnitTests</h3>
<p style="text-align: justify;">При установке <a title="Nuget: Incoding tests helpers" href="https://www.nuget.org/packages/Incoding.MSpecContrib/">Incoding tests helpers</a> в проект добавляется файл MSpecAssemblyContext.cs, в котором настраивается connection к тестовой базе данных.</p>

<pre class="lang:c# decode:true">public class MSpecAssemblyContext : IAssemblyContext
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
                .Mappings(configuration =&gt; configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly));

        PleasureForData.StartNhibernate(configure, true);
    }

    public void OnAssemblyComplete() { }

    #endregion
}</pre>
<h2>Часть 1. Установка.</h2>
<p style="text-align: justify;">Итак, приступим к выполнению поставленной в <em>disclamer </em>задаче - начнем писать наше приложение. Первый этап создания приложения - создание структуры solution'а проекта и добавление projects в него. Solution проекта будет называться <strong>Example </strong>и, как уже было сказано во введении, будет иметь три projects. Начнем с project'а, который будет отвечать за бизнес-логику приложения - с <strong>Domain.</strong></p>
<p style="text-align: justify;">Создаем class library <strong>Domain</strong>.</p>
<p style="text-align: justify;"><a href="http://blog.incframework.com/wp-content/uploads/2015/06/Domain.png"><img class="aligncenter wp-image-1522 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Domain-e1433938652855.png" alt="Domain" width="800" height="553" /></a></p>
<p style="text-align: justify;">Далее перейдем к клиентской части - создаем и устанавливаем как запускаемый пустой проект ASP.NET Web Application <b>UI </b>с сылками на MVC packages.</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/UI1.png"><img class="aligncenter wp-image-1523 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/UI1-e1433938677813.png" alt="UI1" width="800" height="553" /></a>

<a href="http://blog.incframework.com/wp-content/uploads/2015/06/UI2.png"><img class="aligncenter wp-image-1524 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/UI2-e1433938757884.png" alt="UI2" width="770" height="540" /></a>
<p style="text-align: justify;">И наконец, добавим class library <strong>UnitTests, </strong>отвечающую за юнит-тестирование.</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/UnitTests.png"><img class="aligncenter wp-image-1525 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/UnitTests-e1433938798326.png" alt="UnitTests" width="800" height="553" /></a>
<p style="text-align: justify;"><em><strong>Внимание: </strong>хотя юнит-тесты и не являются обязательной частью приложения, мы рекомендуем Вам всегда покрывать код тестами, так как это позволит в будущем избежать множества проблем с ошибками в коде за счет автоматизации тестирования.</em></p>
<p style="text-align: justify;">После выполнения всех вышеперечисленных действий должен получится следующий Solution:</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/Solution.png"><img class="aligncenter wp-image-1527 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Solution-e1433938901524.png" alt="Solution" width="664" height="547" /></a>
<p style="text-align: justify;">После создания структуры Solution'а необходимо собственно установить пакеты <strong>Incoding Framework </strong>из Nuget в соответствующие projects.</p>
<p style="text-align: justify;">Установка происходит через Nuget. Для всех projects алгоритм установки один:</p>

<ol>
	<li style="text-align: justify;">Кликните правой кнопкой по проекту и выберите в контекстном меню пункт <strong>Manage Nuget Packages...</strong></li>
	<li style="text-align: justify;">В поиске введите <strong>incoding</strong></li>
	<li style="text-align: justify;">Выберите нужный пакет и установите его</li>
</ol>
<p style="text-align: justify;">Сначала устанавливаем <a title="Incoding framework" href="https://www.nuget.org/packages/Incoding.Framework/">Incoding framework</a> в <strong>Domain</strong>.</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/Incoding_framework_1.png"><img class="aligncenter wp-image-1530 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Incoding_framework_1-e1433940738743.png" alt="Incoding_framework_1" width="800" height="539" /></a>
<p style="text-align: justify;">Далее добавляем в файл <strong>Domain -&gt; Infrastructure -&gt; Bootstrapper.cs</strong> ссылку на StructureMap.Graph.</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/StructureMap_ref.png"><img class="aligncenter wp-image-1531 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/StructureMap_ref-e1433940776329.png" alt="StructureMap_ref" width="800" height="63" /></a>

В <strong>UI </strong>нужно установить два пакета:
<ol>
	<li><a title="Nuget: Incoding Meta Language" href="https://www.nuget.org/packages/Incoding.MetaLanguage/">Incoding Meta Language</a></li>
	<li><a title="Nuget: Incoding Meta Language Contrib" href="https://www.nuget.org/packages/Incoding.MetaLanguage.Contrib/">Incoding Meta Language Contrib</a></li>
</ol>
<p style="text-align: justify;"><strong><img class="aligncenter wp-image-1539 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Incoding_Meta_Languge-e1433940844592.png" alt="Incoding_Meta_Languge" width="800" height="539" /></strong></p>
<p style="text-align: justify;"><a href="http://blog.incframework.com/wp-content/uploads/2015/06/MetaLanguageContrib_install.png"><img class="aligncenter wp-image-1562 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/MetaLanguageContrib_install-e1433941058675.png" alt="MetaLanguageContrib_install" width="800" height="539" /></a></p>
<p style="text-align: justify;"><b><i>Внимание: </i></b><i>убедитесь, что для References -&gt; System.Web.Mvc.dll свойство "Copy Local" установлено в "true"</i></p>
<p style="text-align: justify;">Теперь файл <strong>Example.UI -&gt; Views -&gt; Shared -&gt; _Layout.cshtml </strong>измените таким образом, чтобы он выглядел так:</p>

<pre class="lang:c# decode:true">@using Incoding.MvcContrib
&lt;!DOCTYPE html&gt;
&lt;html &gt;
&lt;head&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery-1.9.1.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery-ui-1.10.2.min.js")"&gt;&lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/underscore.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery.form.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery.history.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery.validate.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/handlebars-1.1.2.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/incoding.framework.min.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/incoding.meta.language.contrib.js")"&gt; &lt;/script&gt;
    &lt;script type="text/javascript" src="@Url.Content("~/Scripts/bootstrap.min.js")"&gt; &lt;/script&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/bootstrap.min.css")"&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/themes/base/jquery.ui.core.css")"&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/themes/base/jquery.ui.datepicker.css")"&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/themes/base/jquery.ui.dialog.css")"&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/themes/base/jquery.ui.theme.css")"&gt;
    &lt;link rel="stylesheet" type="text/css" href="@Url.Content("~/Content/themes/base/jquery.ui.menu.css")"&gt;
    &lt;script&gt;
        TemplateFactory.Version = '@Guid.NewGuid().ToString()';
    &lt;/script&gt;
&lt;/head&gt;
@Html.Incoding().RenderDropDownTemplate()
&lt;body&gt;
@RenderBody()
&lt;/body&gt;
&lt;/html&gt;</pre>
<p style="text-align: justify;">Осталось добавить ссылку на Bootstrapper.cs в файлы <b>Example.UI -&gt; App_Start -&gt; IncodingStart.cs </b>и <strong>Example.UI -&gt; Controllers -&gt; DispatcherController.cs</strong>.</p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/IncodingStart_bootstrapper.png"><img class="aligncenter wp-image-1540 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/IncodingStart_bootstrapper-e1433941248848.png" alt="IncodingStart_bootstrapper" width="400" height="240" /></a>

<a href="http://blog.incframework.com/wp-content/uploads/2015/06/DispatcherController_bootstrapper.png"><img class="aligncenter wp-image-1541 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/DispatcherController_bootstrapper-e1433941221806.png" alt="DispatcherController_bootstrapper" width="712" height="185" /></a>
<p style="text-align: justify;"><em><strong>Внимание: </strong>если вы используете MVC5, то для работы framework'а необходимо добавить следующий код в файл Web.config</em></p>

<pre class="lang:c# decode:true">&lt;dependentAssembly&gt;
  &lt;assemblyIdentity name="System.Web.Mvc" publicKeyToken="31bf3856ad364e35" culture="neutral" /&gt;
  &lt;bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" /&gt;
&lt;/dependentAssembly&gt;</pre>
<p style="text-align: justify;">Осталось установить <a title="Nuget: Incoding tests helpers" href="https://www.nuget.org/packages/Incoding.MSpecContrib/">Incoding tests helpers</a> в <strong>UnitTests </strong>и добавить ссылку на Bootstrapper.cs в <strong>Example.UnitTests -&gt; MSpecAssemblyContext.cs.</strong></p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/Incoding_tests_helpers.png"><img class="aligncenter wp-image-1542 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Incoding_tests_helpers-e1433941531335.png" alt="Incoding_tests_helpers" width="800" height="539" /></a>

<a href="http://blog.incframework.com/wp-content/uploads/2015/06/MSpecAssemblyContext_bootstrapper.png"><img class="aligncenter wp-image-1544 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/MSpecAssemblyContext_bootstrapper-e1433941543161.png" alt="MSpecAssemblyContext_bootstrapper" width="800" height="116" /></a>
<p style="text-align: justify;">Последний этап подготовки проектов к работе - создание структуры папок для projects.</p>
<p style="text-align: justify;">Добавьте следующие папки в проект <strong>Example.Domain:</strong></p>

<ol style="text-align: justify;">
	<li>Operations - command и query проекта</li>
	<li>Persistences - сущности для маппинга БД</li>
	<li>Specifications - where и order спецификации для фильтрации данных при запросах</li>
</ol>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/Example.Domain_folders.png"><img class="aligncenter wp-image-1557 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/Example.Domain_folders-e1433942308588.png" alt="Example.Domain_folders" width="350" height="154" /></a>
<p style="text-align: justify;">В проекте <strong>Example.UnitTests</strong> создайте такую же структуру папок как и в <strong>Example.Domain.</strong></p>
<a href="http://blog.incframework.com/wp-content/uploads/2015/06/UnitTests_folders.png"><img class="aligncenter wp-image-1558 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/UnitTests_folders-e1433942319802.png" alt="UnitTests_folders" width="310" height="172" /></a>
<h1>Часть 2. Настройка DB connection.</h1>
<p style="text-align: justify;">Для начала создадим БД, с которыми будем работать. Откройте SQL Managment Studio и создайте две базы данных: Example и Example_test.</p>
<p style="text-align: justify;"><a href="http://blog.incframework.com/wp-content/uploads/2015/06/add_DB1.png"><img class="aligncenter wp-image-1597 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/add_DB1-e1434366917892.png" alt="add_DB" width="525" height="291" /></a></p>
<p style="text-align: justify;"><a href="http://blog.incframework.com/wp-content/uploads/2015/06/example_db.png"><img class="aligncenter wp-image-1598 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/example_db-e1434367010409.png" alt="example_db" width="525" height="471" /></a></p>
<p style="text-align: justify;"><a href="http://blog.incframework.com/wp-content/uploads/2015/06/example_test_db.png"><img class="aligncenter wp-image-1599 size-full" src="http://blog.incframework.com/wp-content/uploads/2015/06/example_test_db-e1434367032172.png" alt="example_test_db" width="525" height="471" /></a></p>
<p style="text-align: justify;">Для того чтобы работать с БД в необходимо настроить connection. Добавьте в файлы <strong>Example.UI -&gt; Web.config</strong> и <strong>Example.UnitTests -&gt; app.config</strong> connection string к базе данных:</p>

<pre class="lang:c# decode:true ">  &lt;connectionStrings&gt;
    &lt;add name="Example" connectionString="Data Source=INCODING-PC\SQLEXPRESS;Database=Example;Integrated Security=false; User Id=sa;Password=1" providerName="System.Data.SqlClient" /&gt;
    &lt;add name="Example_Test" connectionString="Data Source=INCODING-PC\SQLEXPRESS;Database=Example_Test;Integrated Security=true" providerName="System.Data.SqlClient" /&gt;
  &lt;/connectionStrings&gt;</pre>
<p style="text-align: justify;">В файле <strong>Example.Domain -&gt; Infrastructure -&gt; Bootstrapper.cs </strong>зарегистрируйте по ключу "Example" соответствующую строку подключения:</p>

<pre class="lang:c# decode:true">//Настройка FluentlyNhibernate
var configure = Fluently
        .Configure()
        .Database(MsSqlConfiguration.MsSql2008.ConnectionString(ConfigurationManager.ConnectionStrings["Example"].ConnectionString))
        .Mappings(configuration =&gt; configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly))
        .ExposeConfiguration(cfg =&gt; new SchemaUpdate(cfg).Execute(false, true))
        .CurrentSessionContext(); //Настройка конфигурации базы данных</pre>
<p style="text-align: justify;">В файле <strong>Example.UnitTests -&gt; MSpecAssemblyContext.cs </strong> зарегистрируйте по ключу "Example_Test" строку подключения к базе данных для тестов:</p>

<pre class="lang:c# decode:true">//Настройка подключения к тестовой БД
var configure = Fluently
        .Configure()
        .Database(MsSqlConfiguration.MsSql2008
                                    .ConnectionString(ConfigurationManager.ConnectionStrings["Example_Test"].ConnectionString)
                                    .ShowSql())
        .Mappings(configuration =&gt; configuration.FluentMappings.AddFromAssembly(typeof(Bootstrapper).Assembly));</pre>
<p style="text-align: justify;"><em><strong>Внимание:</strong> базы данных Example и Example_Test должны существовать.</em></p>

<h1>Часть 3. CRUD.</h1>
<p style="text-align: justify;">После выполнения всех приведенных выше действий мы подошли к самой интересной части - написанию кода, реализующего <strong>C</strong>reate<strong>R</strong>ead<strong>U</strong>pdate<strong>D</strong>elete-функционал приложения. Для начала необходимо создать класс сущности, которая будет маппиться на БД. В нашем случае это будет Human.cs, который добавим в папку <strong>Example.Domain -&gt; Persistences.</strong></p>

<h6>Human.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using Incoding.Data;

    #endregion

    public class Human : IncEntityBase
    {
        #region Properties

        public virtual DateTime Birthday { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string Id { get; set; }

        public virtual string LastName { get; set; }

        public virtual Sex Sex { get; set; }

        #endregion

        #region Nested Classes

        public class Map : NHibernateEntityMap&lt;Human&gt;
        {
            #region Constructors

            protected Map()
            {
                IdGenerateByGuid(r =&gt; r.Id);
                MapEscaping(r =&gt; r.FirstName);
                MapEscaping(r =&gt; r.LastName);
                MapEscaping(r =&gt; r.Birthday);
                MapEscaping(r =&gt; r.Sex);
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
}</pre>
<p style="text-align: justify;">Наш класс содержит несколько полей, в которые мы будем записывать данные, и вложенный класс маппинга (<i>class Map).</i></p>
<p style="text-align: justify;"><em><strong>Заметка:</strong> после создания класса <strong>Human</strong> Вам больше не нужно производить никаких действий (дописывание xml-маппинга) благодаря <a title="Fluent Nhibernate" href="http://www.fluentnhibernate.org/">FluentNhibernate</a>.</em></p>
<p style="text-align: justify;">Теперь добавим команды (Command) и запросы (Query), которые будут отвечать за реализацию CRUD-операций. Первая комманда будет отвечать за добавление новой или изменение существующей записи типа Human. Комманда довольно простая: мы либо получаем из Repository сущность по ключу (Id), либо, если такой сущности нет, создаем новую. В обоих случаях сущность получает значения, которые указаны в свойствах класса AddOrEditHumanCommand. Добавим файл  <strong>Example.Domain -&gt; Operations -&gt; AddOrEditHumanCommand.cs </strong>в проект.</p>

<h6>AddOrEditHumanCommand.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

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

        public override void Execute()
        {
            var human = Repository.GetById&lt;Human&gt;(Id) ?? new Human();

            human.FirstName = FirstName;
            human.LastName = LastName;
            human.Birthday = BirthDay;
            human.Sex = Sex;

            Repository.SaveOrUpdate(human);
        }
    }
}</pre>
<p style="text-align: justify;">Следующая часть CRUD - Read - запрос на чтение сущностей из базы. Добавьте файл <strong>Example.Domain -&gt; Operations -&gt; GetPeopleQuery.cs.</strong></p>

<h6>GetPeopleQuery.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System.Collections.Generic;
    using System.Linq;
    using Incoding.CQRS;

    #endregion

    public class GetPeopleQuery : QueryBase&lt;List&lt;GetPeopleQuery.Response&gt;&gt;
    {
        #region Properties

        public string Keyword { get; set; }

        #endregion

        #region Nested Classes

        public class Response
        {
            #region Properties

            public string Birthday { get; set; }

            public string FirstName { get; set; }

            public string Id { get; set; }

            public string LastName { get; set; }

            public string Sex { get; set; }

            #endregion
        }

        #endregion

        protected override List&lt;Response&gt; ExecuteResult()
        {
            return Repository.Query&lt;Human&gt;().Select(human =&gt; new Response
                                                                 {
                                                                         Id = human.Id,
                                                                         Birthday = human.Birthday.ToShortDateString(),
                                                                         FirstName = human.FirstName,
                                                                         LastName = human.LastName,
                                                                         Sex = human.Sex.ToString()
                                                                 }).ToList();
        }
    }
}</pre>
<p style="text-align: justify;">И оставшаяся часть функционала - это Delete - удаление записей из БД по ключу (Id).  Добавьте файл <strong>Example.Domain -&gt; Operations -&gt; DeleteHumanCommand.cs.</strong></p>

<h6>DeleteHumanCommand.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using Incoding.CQRS;

    #endregion

    public class DeleteHumanCommand : CommandBase
    {
        #region Properties

        public string HumanId { get; set; }

        #endregion

        public override void Execute()
        {
            Repository.Delete&lt;Human&gt;(HumanId);
        }
    }
}</pre>
<p style="text-align: justify;">Для того чтобы наполнить БД начальными данными добавьте файл <strong>Example.Domain -&gt; InitPeople.cs </strong>- этот файл наследуется от интерфейса ISetUp.</p>

<h6 style="text-align: justify;">ISetup</h6>
<pre class="lang:c# decode:true">using System;

namespace Incoding.CQRS
{
  public interface ISetUp : IDisposable
  {
    int GetOrder();

    void Execute();
  }
}</pre>
<p style="text-align: justify;">Все экземпляры классов, унаследованных от ISetUp, регистрируются через IoC в Bootstrapper.cs (был приведен во введении). После регистрации они запускаются на исполнение (public void Execute() ) по порядку (public int GetOrder() ).</p>

<h6>InitPeople.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using Incoding.Block.IoC;
    using Incoding.CQRS;
    using NHibernate.Util;

    #endregion

    public class InitPeople : ISetUp
    {
        public void Dispose() { }

        public int GetOrder()
        {
            return 0;
        }

        public void Execute()
        {
            //получение Dispatcher для выполнения Query и Command
            var dispatcher = IoCFactory.Instance.TryResolve&lt;IDispatcher&gt;();
            
            //не добавлять записи, если в базе есть хотя бы одна запись
            if (dispatcher.Query(new GetEntitiesQuery&lt;Human&gt;()).Any())
                return;

            //добавление записей
            dispatcher.Push(new AddOrEditHumanCommand
                                {
                                        FirstName = "Hellen",
                                        LastName = "Jonson",
                                        BirthDay = Convert.ToDateTime("06/05/1985"),
                                        Sex = Sex.Female
                                });
            dispatcher.Push(new AddOrEditHumanCommand
                                {
                                        FirstName = "John",
                                        LastName = "Carlson",
                                        BirthDay = Convert.ToDateTime("06/07/1985"),
                                        Sex = Sex.Male
                                });
        }
    }
}</pre>
<p style="text-align: justify;">Back-end реализация CRUD готова. Теперь надо добавить клиентский код. Также как и в случае с серверной частью, начнем реализацию с части создания/редактирования записи. <span style="line-height: 1.5;">Добавьте файл </span><strong style="line-height: 1.5;">Example.UI -&gt; Views -&gt; Home -&gt; AddOrEditHuman.cshtml. </strong><span style="line-height: 1.5;">Представленный IML-код создает стандартную html-форму</span> и работает с командой AddOrEditHumanCommand, отправляя на сервер соответствующий Ajax-запрос.</p>

<h6>AddOrEditHuman.cshtml</h6>
<pre class="lang:c# decode:true">@using Example.Domain
@using Incoding.MetaLanguageContrib
@using Incoding.MvcContrib
@model Example.Domain.AddOrEditHumanCommand
@*Формирование формы для Ajax-отправки на выполнение AddOrEditHumanCommand*@
@using (Html.When(JqueryBind.Submit)
            @*Прерывание поведения по умолчанию и отправка формы через Ajax*@
            .PreventDefault()
            .Submit()
            .OnSuccess(dsl =&gt;
                           {
                               dsl.WithId("PeopleTable").Core().Trigger.Incoding();
                               dsl.WithId("dialog").JqueryUI().Dialog.Close();
                           })
            .OnError(dsl =&gt; dsl.Self().Core().Form.Validation.Refresh())
            .AsHtmlAttributes(new
                                  {
                                          action = Url.Dispatcher().Push(new AddOrEditHumanCommand()),
                                          enctype = "multipart/form-data",
                                          method = "POST"
                                  })
            .ToBeginTag(Html, HtmlTag.Form))
{
    &lt;div&gt;
        @Html.HiddenFor(r =&gt; r.Id)
        @Html.ForGroup(r =&gt; r.FirstName).TextBox(control =&gt; control.Label.Name = "First name")
        &lt;br/&gt;
        @Html.ForGroup(r =&gt; r.LastName).TextBox(control =&gt; control.Label.Name = "Last name")
        &lt;br/&gt;
        @Html.ForGroup(r =&gt; r.BirthDay).TextBox(control =&gt; control.Label.Name = "Birthday")
        &lt;br/&gt;
        @Html.ForGroup(r =&gt; r.Sex).DropDown(control =&gt; control.Input.Data = typeof(Sex).ToSelectList())
    &lt;/div&gt;

    &lt;div&gt;
        &lt;input type="submit" value="Save"/&gt;
        @*Закрытие диалога*@
        @(Html.When(JqueryBind.Click)
              .PreventDefault()
              .StopPropagation()
              .Direct()
              .OnSuccess(dsl =&gt; { dsl.WithId("dialog").JqueryUI().Dialog.Close(); })
              .AsHtmlAttributes()
              .ToButton("Cancel"))
    &lt;/div&gt;
}</pre>
<p style="text-align: justify;">Далее следует template, который является шаблоном для загрузки данных, полученных от GetPeopleQuery. Здесь описывается таблица, которая будет отвечать не только за вывод данных, но и за удаление и редактирование отдельных записей: добавьте файл <strong>Example.UI -&gt; Views -&gt; Home -&gt; HumanTmpl.cshtml.</strong></p>

<h6>HumanTmpl.cshtml</h6>
<pre class="lang:c# decode:true">@using Example.Domain
@using Incoding.MetaLanguageContrib
@using Incoding.MvcContrib
@{
    using (var template = Html.Incoding().Template&lt;GetPeopleQuery.Response&gt;())
    {
        &lt;table class="table"&gt;
            &lt;thead&gt;
            &lt;tr&gt;
                &lt;th&gt;
                    First name
                &lt;/th&gt;
                &lt;th&gt;
                    Last name
                &lt;/th&gt;
                &lt;th&gt;
                    Birthday
                &lt;/th&gt;
                &lt;th&gt;
                    Sex
                &lt;/th&gt;
                &lt;th&gt;&lt;/th&gt;
            &lt;/tr&gt;
            &lt;/thead&gt;
            &lt;tbody&gt;
            @using (var each = template.ForEach())
            {
                &lt;tr&gt;
                    &lt;td&gt;
                        @each.For(r =&gt; r.FirstName)
                    &lt;/td&gt;
                    &lt;td&gt;
                        @each.For(r =&gt; r.LastName)
                    &lt;/td&gt;
                    &lt;td&gt;
                        @each.For(r =&gt; r.Birthday)
                    &lt;/td&gt;
                    &lt;td&gt;
                        @each.For(r =&gt; r.Sex)
                    &lt;/td&gt;
                    &lt;td&gt;
                        @*Кнопка открытия диалога для редактирования*@
                        @(Html.When(JqueryBind.Click)
                              .AjaxGet(Url.Dispatcher().Model&lt;AddOrEditHumanCommand&gt;(new
                                                                                         {
                                                                                                 Id = each.For(r =&gt; r.Id),
                                                                                                 FirstName = each.For(r =&gt; r.FirstName),
                                                                                                 LastName = each.For(r =&gt; r.LastName),
                                                                                                 BirthDay = each.For(r =&gt; r.Birthday),
                                                                                                 Sex = each.For(r =&gt; r.Sex)
                                                                                         }).AsView("~/Views/Home/AddOrEditHuman.cshtml"))
                              .OnSuccess(dsl =&gt; dsl.WithId("dialog").Behaviors(inDsl =&gt;
                                                                                   {
                                                                                       inDsl.Core().Insert.Html();
                                                                                       inDsl.JqueryUI().Dialog.Open(option =&gt;
                                                                                                                        {
                                                                                                                            option.Resizable = false;
                                                                                                                            option.Title = "Edit human";
                                                                                                                        });
                                                                                   }))
                              .AsHtmlAttributes()
                              .ToButton("Edit"))
                        @*Кнопка удаления записи*@
                        @(Html.When(JqueryBind.Click)
                              .AjaxPost(Url.Dispatcher().Push(new DeleteHumanCommand() { HumanId = each.For(r =&gt; r.Id) }))
                              .OnSuccess(dsl =&gt; dsl.WithId("PeopleTable").Core().Trigger.Incoding())
                              .AsHtmlAttributes()
                              .ToButton("Delete"))
                    &lt;/td&gt;
                &lt;/tr&gt;
            }
            &lt;/tbody&gt;
        &lt;/table&gt;
    }
}</pre>
Задача открытия диалогового окна достаточно распространена, поэтому код, отвечающий за это действие, можно вынести в <a title="Blog: Extensions" href="http://blog.incframework.com/ru/extensions/">extension</a>.

Последняя часть - изменение стартовой страницы так, чтобы при ее загрузке выполнялся Ajax-запрос на сервер для получения данных от GetPeopleQuery и отображения их через HumanTmpl: измените файл <strong>Example.UI -&gt; Views -&gt; Home -&gt; Index.cshtml </strong>так, чтобы он соответствовал представленному ниже коду.
<h6>Index.cshtml</h6>
<pre class="lang:c# decode:true">@using Example.Domain
@using Incoding.MetaLanguageContrib
@using Incoding.MvcContrib
@{
    Layout = "~/Views/Shared/_Layout.cshtml";
}
&lt;div id="dialog"&gt;&lt;/div&gt;
@*Загрузка записей, полученных из GetPeopleQuery, через HumanTmpl*@
@(Html.When(JqueryBind.InitIncoding)
      .AjaxGet(Url.Dispatcher().Query(new GetPeopleQuery()).AsJson())
      .OnSuccess(dsl =&gt; dsl.Self().Core().Insert.WithTemplateByUrl(Url.Dispatcher().AsView("~/Views/Home/HumanTmpl.cshtml")).Html())
      .AsHtmlAttributes(new { id = "PeopleTable" })
      .ToDiv())
@*Кнопка добавления новой записи*@
@(Html.When(JqueryBind.Click)
      .AjaxGet(Url.Dispatcher().AsView("~/Views/Home/AddOrEditHuman.cshtml"))
      .OnSuccess(dsl =&gt; dsl.WithId("dialog").Behaviors(inDsl =&gt;
                                                           {
                                                               inDsl.Core().Insert.Html();
                                                               inDsl.JqueryUI().Dialog.Open(option =&gt;
                                                                                                {
                                                                                                    option.Resizable = false;
                                                                                                    option.Title = "Add human";
                                                                                                });
                                                           }))
      .AsHtmlAttributes()
      .ToButton("Add new human"))</pre>
<p style="text-align: justify;">В реальных приложениях валидация введенных данных форм - одна из самых частых задач. Поэтому добавим валидацию данных на форму добавления/редактирования сущности Human. Первая часть - добавление серверного кода.<strong> </strong>Добавьте следующий код в AddOrEditHumanCommand как nested class:</p>

<pre class="lang:c# decode:true">#region Nested Classes

public class Validator : AbstractValidator
{
    #region Constructors

    public Validator()
    {
        RuleFor(r =&gt; r.FirstName).NotEmpty();
        RuleFor(r =&gt; r.LastName).NotEmpty();
    }

    #endregion
}

#endregion</pre>
<p style="text-align: justify;">На форме AddOrEditHuman.cshtml мы использовали конструкции вида:</p>

<pre class="lang:c# decode:true">@Html.ForGroup()</pre>
<p style="text-align: justify;">Поэтому нет необходимости дополнительно добавлять</p>

<pre class="lang:c# decode:true">@Html.ValidationMessageFor()</pre>
<p style="text-align: justify;">для полей - <a title="Советы и подсказки" href="http://blog.incframework.com/ru/tips-and-trick/">ForGroup()</a> сделает это за нас.</p>
<p style="text-align: justify;">Таким образом, мы написали код приложения, которое реализует CRUD-функционал для одной сущности БД.</p>

<h1>Часть 4. Specifications - фильтрация данных.</h1>
<p style="text-align: justify;">Еще одна из задач, которые часто встречаются в реальных проектах - фильтрация запрашиваемых данных. В Incoding Framework для удобства написания кода и соблюдения принципа инкапсуляции для фильтрации получаемых в Query данных используются WhereSpecifications. Добавим в написанный код возможность фильтрации получаемых в GetPeopleQuery данных по FirstName и LastName. В первую очередь добавим два файла спецификаций <strong>Example.Domain -&gt; Specifications -&gt; HumanByFirstNameWhereSpec.cs </strong>и <strong>Example.UI -&gt; Specifications -&gt; HumanByLastNameWhereSpec.cs</strong></p>

<h6 style="text-align: justify;">HumanByFirstNameWhereSpec.cs</h6>
<pre class="lang:c# decode:true ">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using System.Linq.Expressions;
    using Incoding;

    #endregion

    public class HumanByFirstNameWhereSpec : Specification
    {
        #region Fields

        readonly string firstName;

        #endregion

        #region Constructors

        public HumanByFirstNameWhereSpec(string firstName)
        {
            this.firstName = firstName;
        }

        #endregion

        public override Expression&lt;Func&lt;Human, bool&gt;&gt; IsSatisfiedBy()
        {
            if (string.IsNullOrEmpty(this.firstName))
                return null;

            return human =&gt; human.FirstName.ToLower().Contains(this.firstName.ToLower());
        }
    }
}</pre>
<h6>HumanByLastNameWhereSpec.cs</h6>
<pre class="lang:c# decode:true">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using System.Linq.Expressions;
    using Incoding;

    #endregion

    public class HumanByLastNameWhereSpec : Specification
    {
        #region Fields

        readonly string lastName;

        #endregion

        #region Constructors

        public HumanByLastNameWhereSpec(string lastName)
        {
            this.lastName = lastName.ToLower();
        }

        #endregion

        public override Expression&lt;Func&lt;Human, bool&gt;&gt; IsSatisfiedBy()
        {
            if (string.IsNullOrEmpty(this.lastName))
                return null;

            return human =&gt; human.LastName.ToLower().Contains(this.lastName);
        }
    }
}</pre>
Теперь используем написанные спецификации в запросе GetPeopleQuery. При помощи связок .Or()/.And() атомарные спецификации можно соединять в более сложные, что помогает использовать созданные спецификации многократно и при их помощи тонко настраивать необходимые фильтры данных (в нашем примере мы используем связку .Or() ).
<h6>GetPeopleQuery.cs</h6>
<pre class="lang:c# decode:true ">namespace Example.Domain
{
    #region &lt;&lt; Using &gt;&gt;

    using System.Collections.Generic;
    using System.Linq;
    using Incoding.CQRS;
    using Incoding.Extensions;

    #endregion

    public class GetPeopleQuery : QueryBase&lt;List&lt;GetPeopleQuery.Response&gt;&gt;
    {
        #region Properties

        public string Keyword { get; set; }

        #endregion

        #region Nested Classes

        public class Response
        {
            #region Properties

            public string Birthday { get; set; }

            public string FirstName { get; set; }

            public string Id { get; set; }

            public string LastName { get; set; }

            public string Sex { get; set; }

            #endregion
        }

        #endregion

        protected override List&lt;Response&gt; ExecuteResult()
        {
            return Repository.Query(whereSpecification: new HumanByFirstNameWhereSpec(Keyword)
                                            .Or(new HumanByLastNameWhereSpec(Keyword)))
                             .Select(human =&gt; new Response
                                                  {
                                                          Id = human.Id,
                                                          Birthday = human.Birthday.ToShortDateString(),
                                                          FirstName = human.FirstName,
                                                          LastName = human.LastName,
                                                          Sex = human.Sex.ToString()
                                                  }).ToList();
        }
    }
}</pre>
<p style="text-align: justify;">И наконец, осталось немного модифицировать Index.cshtml, чтобы добавить поисковую строку, задействующую при запросе поле Keyword для фильтрации данных.</p>

<h6>Index.cshtml</h6>
<pre class="lang:c# decode:true">@using Example.Domain
@using Incoding.MetaLanguageContrib
@using Incoding.MvcContrib
@{
    Layout = "~/Views/Shared/_Layout.cshtml";
}
&lt;div id="dialog"&gt;&lt;/div&gt;
@*При нажатии кнопки Find инициируется событие InitIncoding и PeopleTable выполняет запрос GetPeopleQuery с параметром Keyword*@
&lt;div&gt;
    &lt;input type="text" id="Keyword"/&gt;
    @(Html.When(JqueryBind.Click)
          .Direct()
          .OnSuccess(dsl =&gt; dsl.WithId("PeopleTable").Core().Trigger.Incoding())
          .AsHtmlAttributes()
          .ToButton("Find"))
&lt;/div&gt;

@(Html.When(JqueryBind.InitIncoding)
      .AjaxGet(Url.Dispatcher().Query(new GetPeopleQuery { Keyword = Selector.Jquery.Id("Keyword") }).AsJson())
      .OnSuccess(dsl =&gt; dsl.Self().Core().Insert.WithTemplateByUrl(Url.Dispatcher().AsView("~/Views/Home/HumanTmpl.cshtml")).Html())
      .AsHtmlAttributes(new { id = "PeopleTable" })
      .ToDiv())

@(Html.When(JqueryBind.Click)
      .AjaxGet(Url.Dispatcher().AsView("~/Views/Home/AddOrEditHuman.cshtml"))
      .OnSuccess(dsl =&gt; dsl.WithId("dialog").Behaviors(inDsl =&gt;
                                                           {
                                                               inDsl.Core().Insert.Html();
                                                               inDsl.JqueryUI().Dialog.Open(option =&gt;
                                                                                                {
                                                                                                    option.Resizable = false;
                                                                                                    option.Title = "Add human";
                                                                                                });
                                                           }))
      .AsHtmlAttributes()
      .ToButton("Add new human"))</pre>
<h1>Часть 5. Юнит-тесты.</h1>
<p style="text-align: justify;">Покроем написанный код тестами. Первый тест отвечает за проверку маппинга сущности Human. Файл When_save_Human.cs добавим в папку Persisteces проекта UnitTests.</p>

<h6><b>When_save_Human.cs</b></h6>
<pre class="lang:c# decode:true">namespace Example.UnitTests.Persistences
{
    #region &lt;&lt; Using &gt;&gt;

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(Human))]
    public class When_save_Human : SpecWithPersistenceSpecification
    {
        #region Fields

        It should_be_verify = () =&gt; persistenceSpecification.VerifyMappingAndSchema();

        #endregion
    }
}</pre>
<p style="text-align: justify;">Данный тест работает с тестовой базой данных (Example_test): создается экземпляр класса Human с автоматически заполненными полями, сохраняется в базу, а затем извлекается и сверяется с созданным экземпляром.</p>
<p style="text-align: justify;">Теперь добавим тесты для WhereSpecifications в папку Specifications.</p>

<h6><strong>When_human_by_first_name.cs</strong></h6>
<pre class="lang:c# decode:true ">namespace Example.UnitTests.Specifications
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(HumanByFirstNameWhereSpec))]
    public class When_human_by_first_name
    {
        #region Fields

        Establish establish = () =&gt;
                                  {
                                      Func&lt;string, Human&gt; createEntity = (firstName) =&gt;
                                                                         Pleasure.MockStrictAsObject(mock =&gt;
                                                                                                            mock.SetupGet(r =&gt; r.FirstName)
                                                                                                                .Returns(firstName));

                                      fakeCollection = Pleasure.ToQueryable(createEntity(Pleasure.Generator.TheSameString()),
                                                                            createEntity(Pleasure.Generator.String()));
                                  };

        Because of = () =&gt;
                         {
                             filterCollection = fakeCollection
                                     .Where(new HumanByFirstNameWhereSpec(Pleasure.Generator.TheSameString()).IsSatisfiedBy())
                                     .ToList();
                         };

        It should_be_filter = () =&gt;
                                  {
                                      filterCollection.Count.ShouldEqual(1);
                                      filterCollection[0].FirstName.ShouldBeTheSameString();
                                  };

        #endregion

        #region Establish value

        static IQueryable fakeCollection;

        static List filterCollection;

        #endregion
    }
}</pre>
<h6><strong>When_human_by_last_name.cs</strong></h6>
<pre class="lang:c# decode:true">namespace Example.UnitTests.Specifications
{
    #region &lt;&lt; Using &gt;&gt;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(HumanByLastNameWhereSpec))]
    public class When_human_by_last_name
    {
        #region Fields

        Establish establish = () =&gt;
                                  {
                                      Func&lt;string, Human&gt; createEntity = (lastName) =&gt;
                                                                         Pleasure.MockStrictAsObject(mock =&gt;
                                                                                                            mock.SetupGet(r =&gt; r.LastName)
                                                                                                                .Returns(lastName));

                                      fakeCollection = Pleasure.ToQueryable(createEntity(Pleasure.Generator.TheSameString()),
                                                                            createEntity(Pleasure.Generator.String()));
                                  };

        Because of = () =&gt;
                         {
                             filterCollection = fakeCollection
                                     .Where(new HumanByLastNameWhereSpec(Pleasure.Generator.TheSameString()).IsSatisfiedBy())
                                     .ToList();
                         };

        It should_be_filter = () =&gt;
                                  {
                                      filterCollection.Count.ShouldEqual(1);
                                      filterCollection[0].LastName.ShouldBeTheSameString();
                                  };

        #endregion

        #region Establish value

        static IQueryable fakeCollection;

        static List filterCollection;

        #endregion
    }
}</pre>
<p style="text-align: justify;">Теперь осталось добавить тесты для команды и запроса (папка Operations), причем для команды необходимо добавить два теста: один для проверки создания новой сущности и второй для проверки редактирования уже существующей сущности.</p>

<h6><strong>When_get_people_query.cs</strong></h6>
<pre class="lang:c# decode:true ">namespace Example.UnitTests.Operations
{
    #region &lt;&lt; Using &gt;&gt;

    using System.Collections.Generic;
    using Example.Domain;
    using Incoding.Extensions;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(GetPeopleQuery))]
    public class When_get_people
    {
        #region Fields

        Establish establish = () =&gt;
                                  {
                                      var query = Pleasure.Generator.Invent&lt;GetPeopleQuery&gt;();
                                      //Создание сущности для теста с автоматическим заполнением полей
                                      human = Pleasure.Generator.Invent&lt;Human&gt;();

                                      expected = new List&lt;GetPeopleQuery.Response&gt;();

                                      mockQuery = MockQuery&lt;GetPeopleQuery, List&lt;GetPeopleQuery.Response&gt;&gt;
                                              .When(query)
                                              //"Заглушка" на запрос к репозиторию
                                              .StubQuery(whereSpecification: new HumanByFirstNameWhereSpec(query.Keyword)
                                                                 .Or(new HumanByLastNameWhereSpec(query.Keyword)),
                                                         entities: human);
                                  };

        Because of = () =&gt; mockQuery.Original.Execute();
        
        //Сравнение полученных данных
        It should_be_result = () =&gt; mockQuery.ShouldBeIsResult(list =&gt; list.ShouldEqualWeakEach(new List&lt;Human&gt;() { human },
                                                                                                (dsl, i) =&gt; dsl.ForwardToValue(r =&gt; r.Birthday, human.Birthday.ToShortDateString())
                                                                                                               .ForwardToValue(r =&gt; r.Sex, human.Sex.ToString())
                                                                               ));

        #endregion

        #region Establish value

        static MockMessage&lt;GetPeopleQuery, List&lt;GetPeopleQuery.Response&gt;&gt; mockQuery;

        static List&lt;GetPeopleQuery.Response&gt; expected;

        static Human human;

        #endregion
    }
}</pre>
<h6><strong>When_add_human.cs</strong></h6>
<pre class="lang:c# decode:true">namespace Example.UnitTests.Operations
{
    #region &lt;&lt; Using &gt;&gt;

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(AddOrEditHumanCommand))]
    public class When_add_human
    {
        #region Fields

        Establish establish = () =&gt;
                                  {
                                      var command = Pleasure.Generator.Invent&lt;AddOrEditHumanCommand&gt;();

                                      mockCommand = MockCommand&lt;AddOrEditHumanCommand&gt;
                                              .When(command)
                                              //"Заглушка" на запрос к репозиторию
                                              .StubGetById&lt;Human&gt;(command.Id, null);
                                  };

        Because of = () =&gt; mockCommand.Original.Execute();

        It should_be_saved = () =&gt; mockCommand.ShouldBeSaveOrUpdate&lt;Human&gt;(human =&gt; human.ShouldEqualWeak(mockCommand.Original));

        #endregion

        #region Establish value

        static MockMessage&lt;AddOrEditHumanCommand, object&gt; mockCommand;

        #endregion
    }
}</pre>
<h6><strong>When_edit_human.cs</strong></h6>
<pre class="lang:c# decode:true">namespace Example.UnitTests.Operations
{
    #region &lt;&lt; Using &gt;&gt;

    using Example.Domain;
    using Incoding.MSpecContrib;
    using Machine.Specifications;

    #endregion

    [Subject(typeof(AddOrEditHumanCommand))]
    public class When_edit_human
    {
        #region Fields

        Establish establish = () =&gt;
                                  {
                                      var command = Pleasure.Generator.Invent&lt;AddOrEditHumanCommand&gt;();

                                      human = Pleasure.Generator.Invent&lt;Human&gt;();

                                      mockCommand = MockCommand&lt;AddOrEditHumanCommand&gt;
                                              .When(command)
                                              //"заглушка" на запрос к репозиторию
                                              .StubGetById(command.Id, human);
                                  };

        Because of = () =&gt; mockCommand.Original.Execute();

        It should_be_saved = () =&gt; mockCommand.ShouldBeSaveOrUpdate&lt;Human&gt;(human =&gt; human.ShouldEqualWeak(mockCommand.Original));

        #endregion

        #region Establish value

        static MockMessage&lt;AddOrEditHumanCommand, object&gt; mockCommand;

        static Human human;

        #endregion
    }
}</pre>
<h1>Список материалов для изучения</h1>
<ol>
	<li><a title="Blog: IML, 5 причин использовать" href="http://blog.incframework.com/ru/5-killer-featuer-iml/">IML, 5 причин использовать</a> - клиентские сценарии</li>
	<li><a title="Blog: CQRS расширенный курс" href="http://blog.incframework.com/ru/cqrs-advanced-course/">CQRS расширенный курс</a> - архитектура серверной части</li>
	<li><a title="Blog: MVD" href="http://blog.incframework.com/ru/model-view-dispatcher/">MVD</a> - описание паттерна Model View Dispatcher</li>
	<li><a title="Blog: Мощь селекторов" href="http://blog.incframework.com/ru/power-selector/">IML-селекторы</a> - описание использования селекторов в IML</li>
	<li><a title="Blog:  Расширения" href="http://blog.incframework.com/ru/extensions/">Расширения</a> - помощь написания extensions для соблюдения принципа <a title="Wiki: DRY" href="https://en.wikipedia.org/wiki/Don%27t_repeat_yourself"><strong>D</strong>on't<strong>R</strong>epeat<strong>Y</strong>ourself</a></li>
	<li><a title="Blog: Repository" href="http://blog.incframework.com/ru/repository/">Репозиторий</a> - описание реализации репозитория и приемов работы с ним</li>
	<li><a title="Blog: Do,Action,Insert" href="http://blog.incframework.com/ru/do-action-insert/">Ajax-сценарии</a> - описание работы IML в связке с Ajax</li>
	<li><a title="Blog: Клиентские template" href="http://blog.incframework.com/ru/client-template/">Шаблоны для вставки данных</a></li>
	<li><a title="Blog: IncTesting" href="http://blog.incframework.com/ru/inc-testing/">Тестирование</a> и <a title="Blog: Тестовый сценарий command и query" href="http://blog.incframework.com/ru/command-test-scenario/">тестовые сценарии</a></li>
</ol>
