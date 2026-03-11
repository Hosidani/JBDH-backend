import numpy as np
import logging
from typing import Any, Callable, Dict, Iterable, List, Optional, Tuple

try:
	from sentence_transformers import SentenceTransformer
except ImportError:
	SentenceTransformer = None

embedding_model = None

logger = logging.getLogger(__name__)


def get_embedding_model():
	global embedding_model
	if embedding_model is not None:
		return embedding_model

	if SentenceTransformer is None:
		logger.error("sentence-transformers är inte installerat")
		return None

	try:
		embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
		return embedding_model
	except Exception:
		logger.exception("Kunde inte initiera SentenceTransformer-modellen")
		return None


def build_embedding_text(parts: Iterable[Any], separator: str = " | ") -> str:
	normalized_parts = []
	for part in parts:
		if part is None:
			continue
		text = " ".join(str(part).split())
		if text:
			normalized_parts.append(text)
	return separator.join(normalized_parts).strip()


def build_job_embedding_text(job_data: Dict[str, Any]) -> str:
	if not isinstance(job_data, dict):
		logger.debug("build_job_embedding_text fick ogiltig typ: %s", type(job_data))
		return ""

	text_parts = [
		job_data.get("title") or "",
		job_data.get("company") or "",
		job_data.get("location") or "",
		job_data.get("work_type") or "",
		job_data.get("employment_type") or "",
		job_data.get("description") or "",
		job_data.get("required_skills") or "",
	]

	return build_embedding_text(text_parts)


def vectorize_job_data(job_data: Dict[str, Any]) -> List[float]:
	try:
		text = build_job_embedding_text(job_data)
		if not text:
			logger.debug("vectorize_job_data: tom text, returnerar tom vektor")
			return []
		return vectorize_text(text)
	except Exception:
		logger.exception("vectorize_job_data misslyckades")
		return []


def vectorize_text(text: Optional[str]) -> List[float]:
	try:
		normalized_text = " ".join(str(text or "").split())
		if not normalized_text:
			logger.debug("vectorize_text: tom text, returnerar tom vektor")
			return []

		model = get_embedding_model()
		if model is None:
			return []

		vector = model.encode(normalized_text, convert_to_numpy=True)

		if isinstance(vector, np.ndarray):
			return vector.astype(float).tolist()

		logger.warning("vectorize_text: oväntad vektortyp %s", type(vector))
		return []
	except Exception:
		logger.exception("vectorize_text misslyckades")
		return []


def cosine_similarity(vector_a: List[float], vector_b: List[float]) -> float:
	try:
		if not vector_a or not vector_b:
			return 0.0

		a = np.asarray(vector_a, dtype=float)
		b = np.asarray(vector_b, dtype=float)

		if a.shape != b.shape:
			logger.debug("cosine_similarity: olika dimensioner %s vs %s", a.shape, b.shape)
			return 0.0

		norm_a = np.linalg.norm(a)
		norm_b = np.linalg.norm(b)
		if norm_a == 0.0 or norm_b == 0.0:
			return 0.0

		return float(np.dot(a, b) / (norm_a * norm_b))
	except Exception:
		logger.exception("cosine_similarity misslyckades")
		return 0.0


def rank_by_vector_similarity(
	query_vector: List[float],
	candidates: Iterable[Any],
	vector_getter: Callable[[Any], List[float]],
	min_score: float = 0.2,
	top_k: int = 10,
) -> List[Tuple[Any, float]]:
	if not query_vector:
		logger.debug("rank_by_vector_similarity: query_vector är tom")
		return []

	if top_k <= 0:
		logger.debug("rank_by_vector_similarity: ogiltig top_k=%s", top_k)
		return []

	scored: List[Tuple[Any, float]] = []
	for candidate in candidates:
		try:
			candidate_vector = vector_getter(candidate) or []
		except Exception:
			logger.exception("rank_by_vector_similarity: kunde inte hämta kandidatvektor")
			continue

		score = cosine_similarity(query_vector, candidate_vector)
		if score >= min_score:
			scored.append((candidate, score))

	scored.sort(key=lambda item: item[1], reverse=True)
	return scored[:top_k]