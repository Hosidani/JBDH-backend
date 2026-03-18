# apps/ai_services/ai_message_templates.py
# AI prompt templates for Deepseek/AI integration

class AIMessageTemplates:
    """
    Provides user message templates for AI command IDs.
    Usage: AIMessageTemplates.USER_MESSAGE_TEMPLATES[0].format(CVText="...")
    """
    USER_MESSAGE_TEMPLATES = {
        0: (
            "Analyse this CV in {Language} and tell me which job is the best fit for this candidate. "
            "Return ONLY a valid JSON object with two language versions (English and Swedish). "
            "Each version should have the following fields: "
            '"BestJobTitlesForUser" (a short list, maximum 10 job titles), '
            '"ReasoningBehindJobTitles" (brief), '
            '"HardSkills" (a short list, maximum 10 skills), '
            '"SoftSkills" (a short list, maximum 10 skills), '
            '"Experiences" (a list of objects, each with "title" (string) and "duration" (string, e.g. "3.5 years")), and '
            '"EducationAndCertifications" (a short list, maximum 10 items, brief). '
            "The JSON should have exactly two top-level keys: \"English\" and \"Swedish\". "
            "Respond ONLY with valid, complete JSON. Do NOT truncate or cut off the response."
            "If the response is too long, summarize or shorten lists, but always return valid JSON."
            "\n\nCV:\n{CVText}"
        ),
        1: (
            "SYSTEM INSTRUCTIONS FOR JOB ANALYSIS (Keep identical for caching):\n"
            "You are an expert HR analyst specializing in job advertisement analysis.\n"
            "Always respond with EXACTLY the specified JSON structure.\n"
            "Be precise, concise, and consistent in your analysis.\n"
            "Focus on extracting concrete requirements and skills.\n\n"
            "TASK: Analyze the following job advertisement and extract key information.\n\n"
            "REQUIRED JSON OUTPUT FORMAT (no additional text):\n"
            "{\n"
            '  "JobTitle": ["max 10 relevant job titles"],\n'
            '  "HardRequirements": ["max 10 must-have requirements"],\n'
            '  "GoodToHaves": ["max 10 nice-to-have skills"],\n'
            '  "Experiences": ["max 10 relevant experiences"],\n'
            '  "EducationAndCertifications": ["max 10 education items"],\n'
            '  "WorkArea": ["max 5 industries like IT, Healthcare"],\n'
            '  "Roles": ["max 5 role types like Manager, Developer"],\n'
            '  "JobSummary": "brief job description"\n'
            "}\n\n"
            "JOB ADVERTISEMENT TO ANALYZE:\n{JobAdText}"
        ),
        2: (
            "You are given a job advertisement and a candidate's CV. "
            "Compare the candidate's qualifications, skills, and experiences to the requirements in the job ad. "
            "Be explicit about where the candidate is a strong fit and where there are gaps or missing requirements. "
            "Return ONLY a valid JSON object with the following fields: "
            "\"FitSummary\" (brief summary of fit), "
            "\"MissingRequirements\" (list of gaps or missing requirements), "
            "\"MatchingSkills\" (list of skills from the CV that match the job ad), "
            "\"RelevantExperiences\" (list of relevant experiences from the CV), "
            "\"EducationAndCertifications\" (from the CV, relevant to the job ad). "
            "\n\nJob Ad:\n{JobAdText}\n\nCV:\n{CVText}"
        ),
        4: (
            "You are given a candidate profile summary (from prior AI CV analysis) and a job description. "
            "Compare them and return ONLY valid JSON with these keys: "
            "\"FitSummary\" (short summary), "
            "\"MatchingPoints\" (list of concrete matching points), "
            "\"NonMatchingPoints\" (list of concrete missing/gap points), "
            "\"ImprovementOpportunities\" (list of practical improvements), "
            "\"CoachTips\" (short coaching advice). "
            "No markdown, no extra text, no code fences. "
            "\n\nUserAIResponse:\n{UserAIResponse}\n\nJobDescription:\n{JobDescription}"
        ),
        5: (
            "🎯 **CV vs Jobb Matchningsanalys** 🎯\n\n"
            "Analysera CV och jobbannons SAMULTAN. Ge:\n\n"
            "1. **CV-profession**: main/sub/specialty (t.ex. 'Engineering > Software > Backend')\n"
            "2. **CV-kompetenser**: name/category/importance (1-5)\n"
            "3. **Jobb-profession**: main/sub/specialty\n"
            "4. **Jobb-krav**: name/category/importance (1-5, 5=must-have)\n"
            "5. **Matchningsresultat**:\n"
            "   - totalMatchScore (0-100)\n"
            "   - skillsMatchScore (0-100)\n"
            "   - professionMatchScore (0-100)\n"
            "6. **Sammanfattning** (1-2 meningar)\n"
            "7. **Styrkor** (topp 3-5 matchningar)\n"
            "8. **Brister** (topp 3-5 saknade krav)\n"
            "9. **FitAssessment**: 'Excellent' | 'Good' | 'Fair' | 'Poor'\n\n"
            "📋 **Format (ENDAST JSON, lowercase nycklar):**\n"
            '{{\n'
            '  "cvProfession": {{"main": "...", "sub": "...", "specialty": "..."}},\n'
            '  "cvRequirements": [{{"name": "Python", "category": "skill", "importance": 5}}],\n'
            '  "jobProfession": {{"main": "...", "sub": "...", "specialty": "..."}},\n'
            '  "jobRequirements": [{{"name": "Docker", "category": "skill", "importance": 4}}],\n'
            '  "matchScores": {{"totalMatchScore": 87, "skillsMatchScore": 82, "professionMatchScore": 95}},\n'
            '  "summary": "Kandidaten är stark backendutvecklare med Docker/Python men saknar React frontend.",\n'
            '  "strengths": ["Python expert", "Docker production", "FastAPI erfarenhet"],\n'
            '  "gaps": ["React saknas", "Frontend testing", "TypeScript"],\n'
            '  "fitAssessment": "Good"\n'
            '}}\n\n'
            "CV:\n---CV---\n{cv_text}\n---CV---\n\n"
            "Jobbannons:\n---JOBB---\n{job_ad_text}\n---JOBB---"
        ),
        # Template 7: CV → Profession + Requirements (same structure as job extraction)
        7: (
            "Uppgift: Analysera följande CV och extrahera kandidatens profession och alla kompetenser. "
            "Identifiera profession med tre nivåer: main, sub och specialty baserat på kandidatens bakgrund och erfarenheter. "
            "Till exempel: 'Engineering > Software > Backend', 'Healthcare > Surgery > Cardiac', 'Business > Marketing > Digital'. "
            "Extrahera även alla kompetenser från CV:t (både hårda och mjuka skills): "
            "- name (kort text) "
            "- category (skill, experience, degree, certification, language, soft-skill) "
            "- importance (1-5 baserat på erfarenhetsnivå: 1=nybörjare, 2=grundläggande, 3=medel, 4=avancerad, 5=expert) "
            "Returnera ENDAST valid JSON med dessa nycklar (lowercase): "
            '{{"profession": {{"main": "...", "sub": "...", "specialty": "..."}}, "requirements": [{{"name": "...", "category": "...", "importance": 1-5}}]}} '
            "CV: ---BEGIN--- {JobAdText} ---END---"
        ),
        # Template 8: Job ad → Profession + Requirements (chunked)
        8: (
            "Uppgift: Extrahera huvudkrav och önskade kvalifikationer från följande jobbannons. För varje krav, returnera: "
            "- name (kort text) "
            "- category (ex: skill, experience, degree, certification, language) "
            "- importance (1-5, där 5 = must-have / avgörande, 1 = nice-to-have) "
            "Identifiera även profession för jobbet med tre nivåer: main, sub och specialty. "
            "Returnera ENDAST valid JSON med dessa nycklar (lowercase): "
            '{{"profession": {{"main": "...", "sub": "...", "specialty": "..."}}, "requirements": [{{"name": "...", "category": "...", "importance": 1-5}}]}} '
            "Job ad: ---BEGIN--- {JobAdText} ---END---"
        ),
    }