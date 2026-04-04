# apps/jobs/services/search_params.py
from dataclasses import dataclass
from django.http import QueryDict

@dataclass(frozen=True)
class JobSearchParams:
    description: str = ""
    location: str = ""
    workArea: str = ""
    roles: str = ""
    competence: str = ""
    offset: int = 0
    limit: int = 20

    @classmethod
    def from_request(cls, request) -> "JobSearchParams":
        def get_param(name: str) -> str:
            return request.GET.get(name, "").strip()
        
        def get_int_param(name: str, default: int = 0, max_val: int = None) -> int:
            try:
                val = int(request.GET.get(name, default))
                return min(val, max_val) if max_val else val
            except (ValueError, TypeError):
                return default

        return cls(
            description=get_param("description"),
            location=get_param("location"),
            workArea=get_param("workArea"),
            roles=get_param("roles"),
            competence=get_param("competence"),
            offset=max(0, get_int_param("offset", 0)),
            limit=max(1, min(100, get_int_param("limit", 20))),
        )

    def dict(self):
        return {
            "description": self.description,
            "location": self.location,
            "workArea": self.workArea,
            "roles": self.roles,
            "competence": self.competence,
            "offset": self.offset,
            "limit": self.limit,
        }