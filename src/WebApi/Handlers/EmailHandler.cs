using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApi.DataStore;
using WebApi.Notifications;

namespace WebApi.Handlers
{
    public class EmailHandler : INotificationHandler<ProductAddedNotification>
    {
        private readonly FakeDataStore fakeDataStore;

        public EmailHandler(FakeDataStore fakeDataStore)
        {
            this.fakeDataStore = fakeDataStore;
        }
        public async Task Handle(ProductAddedNotification notification, CancellationToken cancellationToken)
        {
            await fakeDataStore.EventOccured(notification.Product, "Email sent");
            await Task.CompletedTask;
        }
    }
}
