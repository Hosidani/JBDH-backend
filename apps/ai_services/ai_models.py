
from dataclasses import dataclass, field
from typing import Optional, List, Dict, Any
from apps.users.models import User

@dataclass
class AIRequest:
    user: Optional['User']
    command_id: int
    cv_path: Optional[str] = None
    job_ad_url: Optional[str] = None
    job_ad_text: Optional[str] = None
    user_ai_response: Optional[str] = None
    job_ai_response: Optional[str] = None
    language: Optional[str] = None

@dataclass
class AIResponse:
    id: str
    response_text: Optional[str]
    created_at: str

@dataclass
class AIResult:
    success: bool
    error_message: Optional[str] = None
    response: Optional[str] = None

# CVAnalysisResult dataclass for AI CV analysis response
@dataclass
class ExperienceItem:
    title: Optional[str] = None
    duration: Optional[str] = None

@dataclass
class CVAnalysisResult:
    best_job_titles_for_user: Optional[List[str]] = field(default_factory=list)
    hard_skills: Optional[List[str]] = field(default_factory=list)
    soft_skills: Optional[List[str]] = field(default_factory=list)
    experiences: Optional[List[ExperienceItem]] = field(default_factory=list)
    education_and_certifications: Optional[List[str]] = field(default_factory=list)

    @staticmethod
    def from_json(json_str: str) -> 'CVAnalysisResult':
        import json
        data = json.loads(json_str)
        return CVAnalysisResult(
            best_job_titles_for_user=data.get("BestJobTitlesForUser", []),
            hard_skills=data.get("HardSkills", []),
            soft_skills=data.get("SoftSkills", []),
            experiences=[ExperienceItem(**item) for item in data.get("Experiences", [])],
            education_and_certifications=data.get("EducationAndCertifications", [])
        )

# RequirementExtractionResponse dataclass for profession/requirements extraction
@dataclass
class Profession:
    main: Optional[str] = None
    sub: Optional[str] = None
    specialty: Optional[str] = None

@dataclass
class RequirementItem:
    name: Optional[str] = None
    category: Optional[str] = None
    importance: int = 1  # 1-5, .NET compatibility

    @staticmethod
    def _from_dict(d: Dict[str, Any]) -> 'RequirementItem':
        imp = d.get("importance") or d.get("Importance") or 1
        if isinstance(imp, str) and imp.isdigit():
            imp = int(imp)
        elif not isinstance(imp, int):
            imp = 1
        return RequirementItem(
            name=d.get("name") or d.get("Name") or "",
            category=d.get("category") or d.get("Category") or "",
            importance=int(imp),
        )

@dataclass
class RequirementExtractionResponse:
    profession: Optional[Profession] = None
    requirements: Optional[List[RequirementItem]] = field(default_factory=list)

    @staticmethod
    def from_json(json_str: str) -> 'RequirementExtractionResponse':
        import json
        data = json.loads(json_str)
        profession_data = data.get("Profession") or data.get("profession") or {}
        requirements_data = data.get("Requirements") or data.get("requirements") or []
        if profession_data:
            p = profession_data if isinstance(profession_data, dict) else {}
            prof = Profession(
                main=p.get("main") or p.get("Main") or "",
                sub=p.get("sub") or p.get("Sub") or "",
                specialty=p.get("specialty") or p.get("Specialty") or "",
            )
        else:
            prof = None
        reqs = [RequirementItem._from_dict(item) for item in requirements_data]
        return RequirementExtractionResponse(profession=prof, requirements=reqs)