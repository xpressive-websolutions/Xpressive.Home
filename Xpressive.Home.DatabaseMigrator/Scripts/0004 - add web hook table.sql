
create table WebHook (
    Id nvarchar(32) not null,
    GatewayName nvarchar(16) not null,
    DeviceId nvarchar(64) not null,

    constraint PK_WebHook primary key (Id)
)
