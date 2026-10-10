# UmbrashiftECS

===

This is a rewrite of my unfinished metroidvania game, but as an ECS.

## Docker

Build and run:
```bash
docker build -t umbrashift .
docker run -d --rm -p 8080:8080 -v umbrashift-data:/app/Data umbrashift
```