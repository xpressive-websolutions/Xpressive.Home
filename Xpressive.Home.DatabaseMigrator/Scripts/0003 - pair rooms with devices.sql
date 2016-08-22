
create table RoomDevice (
    Gateway nvarchar(16) not null,
    Id nvarchar(64) not null,
    RoomId uniqueidentifier not null,

    constraint PK_RoomDevice primary key (Gateway, Id),
    constraint FK_RoomDevice_Room foreign key (RoomId) references Room (Id)
)
