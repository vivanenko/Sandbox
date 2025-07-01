using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Sandbox.Ordering.Sagas.OrderPayment.MongoDb;

public class OrderPaymentStateClassMap : BsonClassMap<OrderPaymentState>
{
    public OrderPaymentStateClassMap()
    {
        AutoMap();
        MapIdMember(c => c.CorrelationId)
            .SetIdGenerator(GuidGenerator.Instance)
            .SetSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
        // MapMember(c => c.Version).SetElementName("version");
        MapMember(c => c.OrderId)
            .SetSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
        MapMember(c => c.UserId)
            .SetSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
        MapMember(c => c.RequestId)
            .SetSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
    }
}