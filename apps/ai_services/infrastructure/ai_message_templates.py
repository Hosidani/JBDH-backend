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
            "You are given a CV and a job advertisement.\n\n"
            "Your task is to extract structured information to enable semantic matching.\n\n"
            "IMPORTANT:\n"
            "- Return ONLY valid JSON\n"
            "- Do not include explanations, do NOT use markdown, code blocks, or extra formatting\n"
            "- Keep the structure simple and consistent, If the response is too long, shorten lists or summarize, but always return valid, complete JSON.\n\n"
            "Extract the following:\n\n"
            "1. cv_profession:\n"
            "   - main\n"
            "   - sub\n"
            "   - specialty\n\n"
            "2. cv_skills:\n"
            "   - list of objects with:\n"
            "     - name\n"
            "     - category (skill | tool | technology)\n"
            "     - importance (1-5)\n\n"
            "3. job_profession:\n"
            "   - main\n"
            "   - sub\n"
            "   - specialty\n\n"
            "4. job_requirements:\n"
            "   - list of objects with:\n"
            "     - name\n"
            "     - category (skill | tool | technology)\n"
            "     - importance (1-5)\n\n"
            "5. relevant_experiences: []\n"
            "6. summary:\n"
            "   - short 1-2 sentence summary of fit\n\n"
            "7. strengths:\n"
            "   - list (max 5)\n\n"
            "8. gaps:\n"
            "   - list (max 5)\n\n"
            "Return JSON in this format:\n\n"
            "{{{{\n"
            '  "cv_profession": {{{{ "main": "", "sub": "", "specialty": "" }}}},\n'
            '  "cv_skills": [\n'
            '    {{{{ "name": "", "category": "", "importance": 3 }}}}\n'
            "  ],\n"
            '  "job_profession": {{{{ "main": "", "sub": "", "specialty": "" }}}},\n'
            '  "job_requirements": [\n'
            '    {{{{ "name": "", "category": "", "importance": 4 }}}}\n'
            "  ],\n"
            '  "relevant_experiences": [],\n'
            '  "summary": "",\n'
            '  "strengths": [],\n'
            '  "gaps": []\n'
            "}}}}\n\n"
            "CV:\n{cv_text}\n\n"
            "Job:\n{job_ad_text}\n"
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