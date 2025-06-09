// using MassTransit;
//
// namespace Web.Saga;
//
// public class OrderingRoutingSlip(IBus bus)
// {
//     public class Order
//     {
//         public class Item;
//         public class Discount
//         {
//             public decimal Amount { get; set; }
//         }
//         
//         public Guid Id { get; set; }
//         public List<Item> Items { get; set; } = [];
//         public Discount AppliedDiscount { get; set; } = new();
//         public decimal Amount { get; set; }
//     }
//     
//     public async Task RunAsync(Order order)
//     {
//         var builder = new RoutingSlipBuilder(NewId.NextGuid());
//
//         builder.AddActivity("InventoryReservation", new Uri("queue:InventoryReservation_execute"), new
//         {
//             OrderId = order.Id,
//             Items = order.Items
//         });
//
//         builder.AddActivity("Wallet", new Uri("queue:Wallet_execute"), new
//         {
//             OrderId = order.Id,
//             Amount = order.AppliedDiscount.Amount
//         });
//
//         if (order.Amount > 0)
//         {
//             builder.AddActivity("Payment", new Uri("queue:Payment_execute"), new
//             {
//                 OrderId = order.Id,
//                 Amount = order.Amount
//             });
//         }
//         
//         builder.AddActivity("MarkOrderAsPaid", new Uri("queue:MarkOrderAsPaid_execute"), new
//         {
//             OrderId = order.Id
//         });
//
//         await bus.Execute(builder.Build());
//
//     }
// }
//
// // Reservation
// public class InventoryReservationArguments
// {
//     public class ItemDto
//     {
//         public Guid Id { get; set; }
//         public int Quantity { get; set; }
//     }
//     
//     public Guid OrderId { get; set; }
//     public List<ItemDto> Items { get; set; }
// }
//
// public class InventoryReservationLog
// {
//     public Guid ReservationId { get; set; }
// }
//
// public class InventoryReservationActivity : IActivity<InventoryReservationArguments, InventoryReservationLog>
// {
//     public async Task<ExecutionResult> Execute(ExecuteContext<InventoryReservationArguments> context)
//     {
//         var reservationId = Guid.CreateVersion7();
//         Console.WriteLine($"Reservation: {reservationId}");
//         return context.Completed<InventoryReservationLog>(new
//         {
//             ReservationId = reservationId
//         });
//     }
//
//     public async Task<CompensationResult> Compensate(CompensateContext<InventoryReservationLog> context)
//     {
//         Console.WriteLine($"Reservation cancelled: {context.Log.ReservationId}");
//         return context.Compensated();
//     }
// }
//
// // Wallet
// public class WalletArguments
// {
//     public Guid OrderId { get; set; }
//     public decimal Amount { get; set; }
// }
//
// public class WalletLog
// {
//     public Guid OrderId { get; set; }
//     public decimal Amount { get; set; }
// }
//
// public class WalletActivity : IActivity<WalletArguments, WalletLog>
// {
//     public async Task<ExecutionResult> Execute(ExecuteContext<WalletArguments> context)
//     {
//         Console.WriteLine($"Wallet: {context.Arguments.Amount}");
//         return context.Completed(new WalletLog
//         {
//             OrderId = context.Arguments.OrderId,
//             Amount = context.Arguments.Amount
//         });
//     }
//
//     public async Task<CompensationResult> Compensate(CompensateContext<WalletLog> context)
//     {
//         Console.WriteLine($"Wallet cancelled: {context.Log.Amount}");
//         return context.Compensated();
//     }
// }
//
// // Payment
// public class PaymentArguments
// {
//     public Guid OrderId { get; set; }
//     public decimal Amount { get; set; }
// }
//
// public class PaymentLog
// {
//     public Guid OrderId { get; set; }
//     public decimal Amount { get; set; }
// }
//
// public class PaymentActivity : IActivity<PaymentArguments, PaymentLog>
// {
//     public async Task<ExecutionResult> Execute(ExecuteContext<PaymentArguments> context)
//     {
//         Console.WriteLine($"Payment: {context.Arguments.Amount}");
//         return context.Completed(new PaymentLog
//         {
//             OrderId = context.Arguments.OrderId,
//             Amount = context.Arguments.Amount
//         });
//     }
//
//     public async Task<CompensationResult> Compensate(CompensateContext<PaymentLog> context)
//     {
//         Console.WriteLine($"Payment cancelled: {context.Log.Amount}");
//         return context.Compensated();
//     }
// }
//
// // MarkOrderAsPaid
// public class MarkOrderAsPaidArguments
// {
//     public Guid OrderId { get; set; }
// }
//
// public class MarkOrderAsPaidLog
// {
//     public Guid OrderId { get; set; }
// }
//
// public class MarkOrderAsPaidActivity : IActivity<MarkOrderAsPaidArguments, MarkOrderAsPaidLog>
// {
//     public async Task<ExecutionResult> Execute(ExecuteContext<MarkOrderAsPaidArguments> context)
//     {
//         Console.WriteLine($"MarkOrderAsPaid: {context.Arguments.OrderId}");
//         throw new Exception();
//         return context.Completed(new MarkOrderAsPaidLog
//         {
//             OrderId = context.Arguments.OrderId
//         });
//     }
//
//     public async Task<CompensationResult> Compensate(CompensateContext<MarkOrderAsPaidLog> context)
//     {
//         Console.WriteLine($"MarkOrderAsPaid cancelled: {context.Log.OrderId}");
//         return context.Compensated();
//     }
// }