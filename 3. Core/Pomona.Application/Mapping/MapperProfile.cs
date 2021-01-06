using AutoMapper;
using Pomona.Domain.Entity;
using Qrs2.Protos;

namespace Pomona.Application.Mapping
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ItemType, ItemTypeProto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
