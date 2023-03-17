using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pomona.Domain.Entity;
using Pomona.Protos.Cash;
using Pomona.Protos.Common;
using Pomona.Protos.Contract;
using Pomona.Protos.Inventory;
using Pomona.Protos.Person;
using Pomona.Pwa.Shared;
using System;

namespace Pomona.Application.Mapping
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            #region Contract
            CreateMap<ItemType, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractProto, Person>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractProto, Contract>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => src.Date.ToDateTime()))
                .ForMember(dest => dest.DeliveryDate, o => o.MapFrom(src => src.DeliveryDate.ToDateTime()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Contract, ContractProto>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => Timestamp.FromDateTime((src.Date).ToUniversalTime())))
                .ForMember(dest => dest.DeliveryDate, o => o.MapFrom(src => Timestamp.FromDateTime((src.DeliveryDate).ToUniversalTime())))
                .ForMember(dest => dest.IdentificationTypeId, o => o.MapFrom(src => src.Person.IdentificationTypeId))
                .ForMember(dest => dest.IdentificationNumber, o => o.MapFrom(src => src.Person.IdentificationNumber))
                .ForMember(dest => dest.FullName, o => o.MapFrom(src => src.Person.FullName))
                .ForMember(dest => dest.CellPhone, o => o.MapFrom(src => src.Person.CellPhone))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Payment, PaymentProto>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => Timestamp.FromDateTime((src.Date).ToUniversalTime())))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion

            #region Common

            #endregion

            #region Parametric
            CreateMap<Brand, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));

            CreateMap<ItemType, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));

            CreateMap<IdentificationType, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));

            CreateMap<Person, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Name, o => o.MapFrom(src => src.FullName));

            CreateMap<Person, PersonProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));
            #endregion

            #region Inventory


            CreateMap<Item, ItemProto>()
                .ForMember(dest => dest.ItemType, o => o.MapFrom(src => src.ItemType))
                .ForMember(dest => dest.Provider, o => o.MapFrom(src => src.Provider))
                .ForMember(dest => dest.DateOfEntry, o => o.MapFrom(src => Timestamp.FromDateTime((src.DateOfEntry).ToUniversalTime())))
                .ForMember(dest => dest.DateOfSale, o => o.MapFrom(src => src.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.DateOfSale).ToUniversalTime()) : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Watch, ItemProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Item.Id))
                .ForMember(dest => dest.Reference, o => o.MapFrom(src => src.Item.Reference))
                .ForMember(dest => dest.CostValue, o => o.MapFrom(src => src.Item.CostValue))
                .ForMember(dest => dest.DateOfEntry, o => o.MapFrom(src => Timestamp.FromDateTime((src.Item.DateOfEntry).ToUniversalTime())))
                .ForMember(dest => dest.DateOfSale, o => o.MapFrom(src => src.Item.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.Item.DateOfSale).ToUniversalTime()) : null))
                .ForMember(dest => dest.SaleValue, o => o.MapFrom(src => src.Item.SaleValue))
                .ForMember(dest => dest.Description, o => o.MapFrom(src => src.Item.Description))
                .ForMember(dest => dest.Active, o => o.MapFrom(src => src.Item.Active))
                .ForMember(dest => dest.ItemType, o => o.MapFrom(src => src.Item.ItemType))
                .ForMember(dest => dest.Provider, o => o.MapFrom(src => src.Item.Provider))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Jewel, ItemProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Item.Id))
                .ForMember(dest => dest.Reference, o => o.MapFrom(src => src.Item.Reference))
                .ForMember(dest => dest.CostValue, o => o.MapFrom(src => src.Item.CostValue))
                .ForMember(dest => dest.DateOfEntry, o => o.MapFrom(src => Timestamp.FromDateTime((src.Item.DateOfEntry).ToUniversalTime())))
                .ForMember(dest => dest.DateOfSale, o => o.MapFrom(src => src.Item.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.Item.DateOfSale).ToUniversalTime()) : null))
                .ForMember(dest => dest.SaleValue, o => o.MapFrom(src => src.Item.SaleValue))
                .ForMember(dest => dest.Description, o => o.MapFrom(src => src.Item.Description))
                .ForMember(dest => dest.Active, o => o.MapFrom(src => src.Item.Active))
                .ForMember(dest => dest.ItemType, o => o.MapFrom(src => src.Item.ItemType))
                .ForMember(dest => dest.Provider, o => o.MapFrom(src => src.Item.Provider))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<ItemProto, Item>()
                .ForMember(dest => dest.DateOfEntry, o => o.MapFrom(src => src.DateOfEntry.ToDateTime()))
                .ForMember(dest => dest.DateOfSale, o => o.MapFrom(src => src.DateOfSale != null ? src.DateOfSale.ToDateTime() : DateTime.MinValue))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ItemProto, Watch>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ItemProto, Jewel>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Watch, WatchProto>()
                .ForMember(dest => dest.Item, o => o.MapFrom(src => src.Item))
                .ForMember(dest => dest.Brand, o => o.MapFrom(src => src.Brand))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Jewel, JewelProto>()
                .ForMember(dest => dest.Item, o => o.MapFrom(src => src.Item))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion

            #region Cash
            CreateMap<DailyRecord, Record>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => src.Date.ToString(Constants.DateParse)))
                .ForMember(dest => dest.Value, o => o.MapFrom(src => src.Value.ToString()))
                .ForMember(dest => dest.RecordType, o => o.MapFrom(src => src.RecordType.ToString()))
                .ForMember(dest => dest.PaymentMethod, o => o.MapFrom(src => src.PaymentMethod.ToString()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Record, DailyRecord>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => Convert.ToDateTime(src.Date)))
                .ForMember(dest => dest.Value, o => o.MapFrom(src => int.Parse(src.Value)))
                .ForMember(dest => dest.RecordType, o => o.MapFrom(src => src.RecordType.ToString()))
                .ForMember(dest => dest.PaymentMethod, o => o.MapFrom(src => src.PaymentMethod.ToString()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion
        }
    }
}
