#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import { spawn } from 'child_process';
import { platform } from 'os';

const verbose = process.argv.indexOf('--verbose') != -1;

async function main() {
    fs.cpSync('objects', 'artifacts', {recursive: true});
    const objects = await getObjects('artifacts');
    for (const obj of objects) {
        const images = obj.images;
	if (images === undefined) {
            continue;
	}
	var requiresProcessing = true;
        if (typeof images[Symbol.iterator] === 'function') {
	    if (images.length === 0) requiresProcessing = false;
            for (const image of images) {
                if (typeof image === 'string') {
                    requiresProcessing = false;
		}    
	    }
        } else {
            requiresProcessing = false;
	}
	if (requiresProcessing) {
            reprocessObject(obj);
	}
    }
    for (const obj in objects) {
	var fullPath = path.join(obj.cwd, 'object.json');
        fs.stat(fullPath, (err, stat) => {
            if (stat) {
                // Zip the file into a parkobj
            }
        );
        console.log();
    }
    // Zip everything into a objects.zip
}

async function getObjects(dir) {
    const result = [];
    const files = await getAllFiles(dir);
    for (const file of files) {
        const jsonRegex = /^.+\..+\.json$/;
        if (jsonRegex.test(file)) {
            const cwd = path.dirname(file);
            const obj = await readJsonFile(file);
            obj.cwd = cwd;
            result.push(obj);
        }
    }
    return result;
}

async function reprocessObject(obj) {
    console.log(`Reprocessing ${obj.id}`);
    var cwd = obj.cwd;
    //startProcess(path.join(process.cwd(),'gxc'), [''],obj.cwd);
    await writeJsonFile(path.join(obj.cwd, 'images.json'), obj.images);
    await compileGx(obj.cwd, 'images.json', 'images.dat');
    var imageCount = await getGxImageCount(obj.cwd, 'images.dat');
    console.log(`Num Images ${imageCount}`);
    obj.images = `$LGX:images.dat[0..${imageCount - 1}]`;
    obj.cwd = undefined;
    await writeJsonFile(path.join(cwd, 'object.json'), obj);
    obj.cwd = cwd;
    rm(path.join(cwd, 'images.json'));
    rm(path.join(cwd, 'images'));
}

function compileGx(cwd, manifest, outputFile) {
    return startProcess(path.join(process.cwd(), 'gxc'), ['build', outputFile, manifest], cwd);
}

async function getGxImageCount(cwd, inputFile) {
    const stdout = await startProcess(path.join(process.cwd(), 'gxc'), ['details', inputFile], cwd);
    const result = stdout.match(/numEntries: ([0-9]+)/);
    if (result) {
        return parseInt(result[1]);
    } else {
        throw new Error(`Unable to get number of images for gx file: ${inputFile}`);
    }
}

function readJsonFile(path) {
    return new Promise((resolve, reject) => {
        fs.readFile(path, 'utf8', (err, data) => {
            if (err) {
                reject(err);
            } else {
                resolve(JSON.parse(data));
            }
        });
    });
}

function writeJsonFile(path, data) {
    return new Promise((resolve, reject) => {
        const json = JSON.stringify(data, null, 4) + '\n';
        fs.writeFile(path, json, 'utf8', err => {
            if (err) {
                reject(err);
            } else {
                resolve();
            }
        });
    });
}

function getAllFiles(root) {
    return new Promise((resolve, reject) => {
        const results = [];
        let pending = 0;
        const find = (root) => {
            pending++;
            fs.readdir(root, (err, fileNames) => {
                // if (err) {
                //     reject(err);
                // }
                for (const fileName of fileNames) {
                    const fullPath = path.join(root, fileName);
                    pending++;
                    fs.stat(fullPath, (err, stat) => {
                        // if (err) {
                        //     reject(err);
                        // }
                        if (stat) {
                            if (stat.isDirectory()) {
                                find(fullPath);
                            } else {
                                results.push(fullPath);
                            }
                        }
                        pending--;
                        if (pending === 0) {
                            resolve(results);
                        }
                    });
                }
                pending--;
                if (pending === 0) {
                    resolve(results.sort());
                }
            });
        };
        find(root);
    });
}

function zip(cwd, outputFile, paths) {
    if (platform() == 'win32') {
        return startProcess('7z', ['a', '-tzip', outputFile, ...paths], cwd);
    } else {
        return startProcess('zip', [outputFile, ...paths], cwd);
    }
}

function startProcess(name, args, cwd) {
    return new Promise((resolve, reject) => {
        const options = {};
        if (cwd) options.cwd = cwd;
        if (verbose) {
            console.log(`Launching \"${name} ${cwd} ${args.join(' ')}\"`);
        }
        const child = spawn(name, args, options);
        let stdout = '';
        child.stdout.on('data', data => {
            stdout += data;
        });
        child.stderr.on('data', data => {
            stdout += data;
        });
        child.on('error', err => {
            if (err.code == 'ENOENT') {
                reject(new Error(`${name} was not found`));
            } else {
                reject(err);
            }
        });
        child.on('close', code => {
            if (code !== 0) {
                reject(new Error(`${name} failed:\n${stdout}`));
            } else {
                resolve(stdout);
            }
        });
    });
}

function rm(filename) {
    if (verbose) {
        console.log(`Deleting ${filename}`)
    }
    return new Promise((resolve, reject) => {
        fs.stat(filename, (err, stat) => {
            if (err) {
                if (err.code == 'ENOENT') {
                    resolve();
                } else {
                    reject();
                }
            } else {
                if (stat.isDirectory()) {
                    fs.rm(filename, { recursive: true }, err => {
                        if (err) {
                            reject(err);
                        }
                        resolve();
                    });
                } else {
                    fs.unlink(filename, err => {
                        if (err) {
                            reject(err);
                        }
                        resolve();
                    });
                }
            }
        });
    });
}

try {
    await main();
} catch (err) {
    console.log(err.message);
    process.exitCode = 1;
}
