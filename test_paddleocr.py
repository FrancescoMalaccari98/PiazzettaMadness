from paddleocr import PaddleOCR

IMAGE_PATH = r"C:\Temp\cropverify\crops\105-game.periodIntervalScores.png"

ocr = PaddleOCR(
    use_doc_orientation_classify=False,
    use_doc_unwarping=False,
    use_textline_orientation=False,
    engine="paddle",
)

result = ocr.predict(IMAGE_PATH)

for res in result:
    res.print()
    res.save_to_json("paddle_output")
    res.save_to_img("paddle_output")