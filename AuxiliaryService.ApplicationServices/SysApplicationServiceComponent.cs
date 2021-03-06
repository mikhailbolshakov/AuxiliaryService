﻿using AuxiliaryService.API.ContextInitializer;
using AuxiliaryService.API.Shared.Integration.Rest;
using AuxiliaryService.API.Shared.Integration.Smtp;
using AuxiliaryService.API.Shared.IntegrationMap;
using AuxiliaryService.API.Shared.Links;
using AuxiliaryService.API.Shared.Notification.Email;
using AuxiliaryService.API.Shared.StateMachine;
using AuxiliaryService.ApplicationServices.Shared.ContextInitializer;
using AuxiliaryService.ApplicationServices.Shared.Integration.Rest;
using AuxiliaryService.ApplicationServices.Shared.Integration.Smtp;
using AuxiliaryService.ApplicationServices.Shared.IntegrationMap;
using AuxiliaryService.ApplicationServices.Shared.Links;
using AuxiliaryService.ApplicationServices.Shared.Notification.Consumer;
using AuxiliaryService.ApplicationServices.Shared.Notification.Email;
using AuxiliaryService.ApplicationServices.Shared.StateMachine;
using AuxiliaryService.Domain.Notification.DomainEvent;
using AuxiliaryService.Domain.Notification.Messages;
using AuxiliaryService.Domain.Notification.Providers.Email.Settings;
using AuxiliaryService.Domain.Settings;

namespace AuxiliaryService.ApplicationServices
{
    public class SysApplicationServiceComponent : IocComponent
    {
        public override void Configure()
        {
            Mappings();
            ApplicationServices();
        }

        public override void Initialize()
        {
            var configurationRegistrator = KernelInstance.Get<IConfigurationSettingRegistrator>();
            var factory = KernelInstance.Get<IEmailNotificationSettingsFactory>();
            configurationRegistrator.RegisterConfigurationSettingsFactory(factory);

            var sysFactory = KernelInstance.Get<ISystemSettingsFactory>();
            configurationRegistrator.RegisterConfigurationSettingsFactory(sysFactory);

            // TODO
            //RegisterEventHandlers();
            //RegisterMessageConsumers();
        }

        private void ApplicationServices()
        {
            Bind(typeof(IStateMachineConfiguration<>)).To(typeof(StateMachineConfiguration<>));

            Bind<IRestClient>().To<RestClient>().InSingletonScope();
            Bind<ISmtpClient>().To<SmtpClient>().InSingletonScope();

            Bind<IApplicationContextInitializerFactory>().To<ApplicationContextInitializerFactory>().InSingletonScope();
            Bind<ApplicationContextInitializer>().ToSelf();

            Bind<IEmailNotificationService>().To<EmailNotificationService>().InSingletonScope();

        }

        private void Mappings()
        {
        }

        private void RegisterEventHandlers()
        {
            var register = KernelInstance.Get<IDomainEventHandlerRegistry>();

            var serviceMessagePersistenceService = KernelInstance.Get<IServiceMessagePersistenceService>();
            var serviceMessagePublisher = KernelInstance.Get<QueueServiceMessagePublisher>();
            var moduleName = Framework.ToString();

            var notificationHandler = new ReliableImmediateDomainEventHandler<NotificationDomainEvent, NotificationDomainEventMsg>(
                serviceMessagePersistenceService,
                serviceMessagePublisher,
                (e) =>
                {
                    return new NotificationDomainEventMsg(e.NotificationId, e.RefId, e.ProviderType);
                },
                moduleName
            );

            register.Register(notificationHandler);
        }

        private void RegisterMessageConsumers()
        {
            var container = KernelInstance.Get<IConsumerContainer>();
            var contextInitializerFactory = KernelInstance.Get<IApplicationContextInitializerFactory>();

            var sendNotificationConsumer = new SendNotificationConsumer(
                KernelInstance.Get<IDocumentSerializer>(),
                KernelInstance.Get<MessageQueueConfiguration>(),
                new ConventionBasedEndpointResolver(),
                contextInitializerFactory.CreateInitializer(AdministratorConsts.AdministratorUserId, AdministratorConsts.IntegrationActorCode),
                KernelInstance.Get<IEmailNotificationService>()
            );

            container.Add(sendNotificationConsumer);
        }
    }
}