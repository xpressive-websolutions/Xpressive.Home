
update Variable
set Name = substring(name, 0, 28) + '.Presence'
where Name like 'Philips%.Presence'
