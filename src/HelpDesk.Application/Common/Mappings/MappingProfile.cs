using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Department, DepartmentDto>();
        CreateMap<Ticket, TicketDto>();
        CreateMap<Comment, CommentDto>();
        CreateMap<Attachment, AttachmentDto>()
            .ForMember(d => d.Url, o => o.MapFrom(s => $"/api/attachments/{s.Id}/download"));
        CreateMap<ActivityEvent, ActivityDto>();
        CreateMap<Notification, NotificationDto>();
    }
}
